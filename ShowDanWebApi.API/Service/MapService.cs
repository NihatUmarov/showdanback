using Itinero;
using Itinero.IO.Osm;
using Itinero.Osm.Vehicles;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using OsmSharp.Streams;
using ShowDanWebApi.Core.Entities;
using ShowDanWebApi.Core.Language;
using ShowDanWebApi.Data;

namespace ShowDanWebApi.API.Service;

public interface IMapService
{
    Itinero.Route CalculateRoute(float startLat, float startLon, float endLat, float endLon);
    Task<int> IdentifyCityIdAsync(AppDbContext context, double lat, double lon); // ИСПРАВЛЕНО: возвращает int
    Task<List<OsmAddress>> SearchAddressByTextAsync(AppDbContext context, string searchText);
    Task<OsmAddress?> GetAddressByCoordinatesAsync(AppDbContext context, double lat, double lon, double maxDistanceMeters = 200);
}

public class MapService : IMapService
{
    private readonly Router _router;
    private readonly RouterDb _routerDb;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MapService> _logger;

    public MapService(IHostEnvironment env, IServiceScopeFactory scopeFactory, ILogger<MapService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        string appDataPath = Path.Combine(env.ContentRootPath, "AppData");
        string pbfPath = Path.Combine(appDataPath, "map.osm.pbf");
        string dbPath = Path.Combine(appDataPath, "map.routerdb");

        if (File.Exists(dbPath))
        {
            using var stream = File.OpenRead(dbPath);
            _routerDb = RouterDb.Deserialize(stream);
        }
        else
        {
            _routerDb = new RouterDb();
            if (!File.Exists(pbfPath)) throw new FileNotFoundException($"OSM файл не найден по пути: {pbfPath}");

            using var stream = File.OpenRead(pbfPath);
            _routerDb.LoadOsmData(stream, Vehicle.Car);
            using var outStream = File.Create(dbPath);
            _routerDb.Serialize(outStream);
        }
        _router = new Router(_routerDb);

        Task.Run(() => InitializeAddressDatabaseAsync(pbfPath));
    }

    private async Task InitializeAddressDatabaseAsync(string pbfPath)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            if (await context.OsmAddresses.AnyAsync()) return;

            var geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
            var addressesToInsert = new List<OsmAddress>(2000);
            var globalProcessedKeys = new HashSet<string>(StringComparer.Ordinal);
            var globalProcessedIds = new HashSet<long>();

            using var fileStream = File.OpenRead(pbfPath);
            var source = new PBFOsmStreamSource(fileStream);
            var completeSource = source.ToComplete();

            foreach (var element in completeSource)
            {
                if (element.Tags == null || !element.Tags.ContainsKey("addr:housenumber")) continue;
                if (!globalProcessedIds.Add(element.Id)) continue;

                Coordinate? centerCoordinate = null;

                if (element is OsmSharp.Node node && node.Longitude.HasValue && node.Latitude.HasValue)
                {
                    centerCoordinate = new Coordinate(node.Longitude.Value, node.Latitude.Value);
                }
                else if (element is OsmSharp.Complete.CompleteWay completeWay && completeWay.Nodes?.Any() == true)
                {
                    double sumLat = 0, sumLon = 0;
                    int validNodesCount = 0;

                    foreach (var n in completeWay.Nodes)
                    {
                        if (n.Latitude.HasValue && n.Longitude.HasValue)
                        {
                            sumLat += n.Latitude.Value;
                            sumLon += n.Longitude.Value;
                            validNodesCount++;
                        }
                    }
                    if (validNodesCount > 0)
                        centerCoordinate = new Coordinate(sumLon / validNodesCount, sumLat / validNodesCount);
                }

                if (centerCoordinate == null) continue;

                var country = ExtractMultiLang(element.Tags, "addr:country") ?? new MultiLang("Узбекистан", "Uzbekistan", "O'zbekiston");
                var city = ExtractMultiLang(element.Tags, "addr:city");
                var street = ExtractMultiLang(element.Tags, "addr:street") ?? ExtractMultiLang(element.Tags, "addr:place");
                string house = element.Tags.GetValue("addr:housenumber") ?? "";

                string baseCity = city?.Ru ?? city?.Uz ?? city?.En ?? "";
                string baseStreet = street?.Ru ?? street?.Uz ?? street?.En ?? "";
                string uniqueKey = $"{country.Ru}_{baseCity}_{baseStreet}_{house}".Replace(" ", "").ToLower();

                if (!globalProcessedKeys.Add(uniqueKey)) continue;

                var searchTerms = new HashSet<string>(StringComparer.Ordinal);
                AddMultiLangToSearch(searchTerms, country);
                AddMultiLangToSearch(searchTerms, city);
                AddMultiLangToSearch(searchTerms, street);
                if (!string.IsNullOrWhiteSpace(house)) searchTerms.Add(house);

                addressesToInsert.Add(new OsmAddress
                {
                    Id = element.Id,
                    Country = country,
                    City = city,
                    Street = street,
                    HouseNumber = house,
                    FullSearchAddress = string.Join(" ", searchTerms).ToLower(),
                    Location = geometryFactory.CreatePoint(centerCoordinate)
                });

                if (addressesToInsert.Count >= 2000)
                {
                    await context.OsmAddresses.AddRangeAsync(addressesToInsert);
                    await context.SaveChangesAsync();
                    addressesToInsert.Clear();
                }
            }

            if (addressesToInsert.Count > 0)
            {
                await context.OsmAddresses.AddRangeAsync(addressesToInsert);
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка фонового импорта адресов OSM");
        }
    }

    private MultiLang? ExtractMultiLang(OsmSharp.Tags.TagsCollectionBase tags, string baseKey)
    {
        if (tags == null) return null;

        bool hasBase = tags.TryGetValue(baseKey, out var baseVal);
        tags.TryGetValue($"{baseKey}:ru", out var ruVal);
        tags.TryGetValue($"{baseKey}:en", out var enVal);
        tags.TryGetValue($"{baseKey}:uz", out var uzVal);

        if (!hasBase && ruVal == null && enVal == null && uzVal == null) return null;

        return new MultiLang(
            ru: ruVal ?? baseVal ?? string.Empty,
            en: enVal ?? baseVal ?? string.Empty,
            uz: uzVal ?? baseVal ?? string.Empty
        );
    }

    private void AddMultiLangToSearch(HashSet<string> searchTerms, MultiLang? langItem)
    {
        if (langItem == null) return;
        if (!string.IsNullOrWhiteSpace(langItem.Ru)) searchTerms.Add(langItem.Ru.Trim());
        if (!string.IsNullOrWhiteSpace(langItem.En)) searchTerms.Add(langItem.En.Trim());
        if (!string.IsNullOrWhiteSpace(langItem.Uz)) searchTerms.Add(langItem.Uz.Trim());
    }

    public Itinero.Route CalculateRoute(float startLat, float startLon, float endLat, float endLon)
    {
        var profile = Vehicle.Car.Fastest();
        var start = _router.TryResolve(profile, startLat, startLon, 2000);
        var end = _router.TryResolve(profile, endLat, endLon, 2000);
        if (start.IsError || end.IsError) return null!;

        return _router.Calculate(profile, start.Value, end.Value);
    }

    public async Task<int> IdentifyCityIdAsync(AppDbContext context, double lat, double lon)
    {
        var gf = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        var userLocation = gf.CreatePoint(new Coordinate(lon, lat));

        // ИСПРАВЛЕНО: Никакого c.Code. Вытаскиваем чистый int c.CityId, сортируя по дистанции
        return await context.Cities
            .AsNoTracking()
            .OrderBy(c => c.Location.Distance(userLocation))
            .Select(c => c.CityId)
            .FirstOrDefaultAsync();
    }

    public async Task<List<OsmAddress>> SearchAddressByTextAsync(AppDbContext context, string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText)) return [];
        string cleanSearch = searchText.ToLower().Trim();

        return await context.OsmAddresses
            .AsNoTracking()
            .Where(x => x.FullSearchAddress.Contains(cleanSearch))
            .Take(5)
            .ToListAsync();
    }

    public async Task<OsmAddress?> GetAddressByCoordinatesAsync(AppDbContext context, double lat, double lon, double maxDistanceMeters = 200)
    {
        var gf = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        var userLocation = gf.CreatePoint(new Coordinate(lon, lat));

        return await context.OsmAddresses
            .AsNoTracking()
            .Where(x => EF.Functions.IsWithinDistance(x.Location, userLocation, maxDistanceMeters, true))
            .OrderBy(x => EF.Functions.DistanceKnn(x.Location, userLocation))
            .FirstOrDefaultAsync();
    }
}
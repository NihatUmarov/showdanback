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
    Task<OsmAddress?> GetAddressByCoordinatesAsync(AppDbContext context, double lat, double lon, double maxDistanceMeters = 200);
}

public class MapService : IMapService
{
    private async Task InitializeAddressDatabaseAsync(string pbfPath)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            if (await context.OsmAddresses.AnyAsync()) return;

            var geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
            

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка фонового импорта адресов OSM");
        }
    }

    private MultiLang? ExtractMultiLang(OsmSharp.Tags.TagsCollectionBase tags, string baseKey)
    {
        if (tags == null) return null;

        );
    }

    private void AddMultiLangToSearch(HashSet<string> searchTerms, MultiLang? langItem)
    {
        if (langItem == null) return;
        
    }

    public Itinero.Route CalculateRoute(float startLat, float startLon, float endLat, float endLon)
    {
        var profile = Vehicle.Car.Fastest();
    }

    public async Task<int> IdentifyCityIdAsync(AppDbContext context, double lat, double lon)
    {
        var gf = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        var userLocation = gf.CreatePoint(new Coordinate(lon, lat));

            .FirstOrDefaultAsync();
    }



    public async Task<OsmAddress?> GetAddressByCoordinatesAsync(AppDbContext context, double lat, double lon, double maxDistanceMeters = 200)
    {
        var gf = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        var userLocation = gf.CreatePoint(new Coordinate(lon, lat));

    }
}
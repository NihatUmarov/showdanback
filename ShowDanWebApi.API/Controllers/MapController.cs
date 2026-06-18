using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders.Physical;
using NetTopologySuite.Geometries;
using ShowDanWebApi.API.Service;
using ShowDanWebApi.Data;
using System.Text.Json.Serialization;

namespace ShowDanWebApi.API.Controllers;

[Authorize]
[Route("api/map")]
public class MapController : BaseController
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly IMemoryCache _cache;
    private readonly IMapService _mapService;
    private const string CacheKey = "MapStyleJson";

    public MapController(IWebHostEnvironment env, IMemoryCache cache, AppDbContext context, IMapService mapService) =>
        (_env, _cache, _context, _mapService) = (env, cache, context, mapService);

    [HttpGet("test-tashkent-highway")]
    public IActionResult TestTashkentHighway()
    {
        var route = _mapService.CalculateRoute(41.276f, 69.291f, 41.0131f, 69.3567f);
        return Ok(new { distanceMeters = route.TotalDistance, timeSeconds = route.TotalTime });
    }

    [HttpPost("get-city")]
    public async Task<IActionResult> IdentifyCity([FromBody] GetCityRequest request)
    {
        var gf = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        var userLocation = gf.CreatePoint(new Coordinate(request.Lon, request.Lat));
        var detectedCity = await _context.Cities
            .Select(c => new { c.CityId, Distance = c.Location.Distance(userLocation) * 111139 })
            .OrderBy(x => x.Distance)
            .FirstOrDefaultAsync();

        if (detectedCity == null) return NotFound();
        return Ok(new { code = detectedCity.CityId });
    }

    [LocalizationRequired]
    [HttpGet("getmap_style")]
    public async Task<IActionResult> GetStyle()
    {
        var rawJson = await _cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            var filePath = Path.Combine(_env.ContentRootPath, "AppData", "map.json");
            if (!System.IO.File.Exists(filePath)) return null;

            var fileInfo = new FileInfo(filePath);
            entry.AddExpirationToken(new PollingFileChangeToken(fileInfo)).SetPriority(CacheItemPriority.NeverRemove);

            return await System.IO.File.ReadAllTextAsync(filePath);
        });

        if (rawJson == null) return NotFound();

        string langFieldName = CurrentLang switch { "en" => "name:en", "ru" => "name:ru", "uz" => "name:en", _ => "name" };
        return Content(rawJson.Replace("{{MAP_LANG}}", langFieldName), "application/json");
    }

    [LocalizationRequired]
    [HttpPost("search-address")]
    public async Task<IActionResult> SearchAddress([FromBody] AddressSearchRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Query) || req.Query.Length < 3) return BadRequest();

        var results = await _mapService.SearchAddressByTextAsync(_context, req.Query);

        return Ok(results.Select(a => new AddressResponseDto
        {
            Id = a.Id,
            City = a.City?.Get(CurrentLang) ?? string.Empty,
            Street = a.Street?.Get(CurrentLang) ?? string.Empty,
            HouseNumber = a.HouseNumber ?? string.Empty,
            Lat = a.Location.Y,
            Lon = a.Location.X
        }).ToList());
    }

    [LocalizationRequired]
    [HttpPost("reverse-geocode")]
    public async Task<IActionResult> ReverseGeocodeJson([FromBody] ReverseGeocodeRequest req)
    {
        var address = await _mapService.GetAddressByCoordinatesAsync(_context, req.Lat, req.Lon, req.MaxDistance ?? 300);
        if (address == null) return NotFound();

        return Ok(new AddressResponseDto
        {
            Id = address.Id,
            City = address.City?.Get(CurrentLang) ?? string.Empty,
            Street = address.Street?.Get(CurrentLang) ?? string.Empty,
            HouseNumber = address.HouseNumber ?? string.Empty,
            Lat = address.Location.Y,
            Lon = address.Location.X
        });
    }
}

public record GetCityRequest(double Lat, double Lon);
public record AddressSearchRequest([property: JsonPropertyName("q")] string Query);
public record ReverseGeocodeRequest(
    [property: JsonPropertyName("lat")] double Lat,
    [property: JsonPropertyName("lon")] double Lon,
    [property: JsonPropertyName("dist")] double? MaxDistance = 200
);

public class AddressResponseDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("cty")] public string City { get; set; } = string.Empty;
    [JsonPropertyName("str")] public string Street { get; set; } = string.Empty;
    [JsonPropertyName("hn")] public string HouseNumber { get; set; } = string.Empty;
    [JsonPropertyName("lat")] public double Lat { get; set; }
    [JsonPropertyName("lon")] public double Lon { get; set; }
}
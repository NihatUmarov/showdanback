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


    [LocalizationRequired]
    [HttpGet("getmap_style")]
    public async Task<IActionResult> GetStyle()
    {
        var rawJson = await _cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            var filePath = Path.Combine(_env.ContentRootPath, "Ap
            Lat = address.Location.Y,
            Lon = address.Location.X
        });
    }
}
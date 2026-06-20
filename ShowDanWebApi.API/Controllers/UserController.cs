using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShowDanWebApi.API.Service;
using ShowDanWebApi.Core.DTO;
using ShowDanWebApi.Core.Entities.Users;
using ShowDanWebApi.Data;
using System.Text.Json;

namespace ShowDanWebApi.API.Controllers;

[Authorize]
[Route("api/user")]
public class UserController : BaseController
{
    private readonly AppDbContext _context;
    private readonly IImageProcessingService _imageService;

    public UserController(AppDbContext db, IImageProcessingService imageService)
    {
        _context = db;
        _imageService = imageService;
    }

    [HttpPost("manage")]
    public async Task<IActionResult> ManagePersonalInfo([FromForm] ManagePersonalRequest request)
    {
       
                u.UserId,
                u.FirstName,
                u.LastName,
                u.Balance,
                u.CurrencyCode,
                u.PhotoThumbUrl,
                Services = u.PerformerProfile != null
                    ? u.PerformerProfile.PerformerServices.Select(s => new { s.ServiceId, s.StageName, s.CategoryId }).ToList()
           

        if (totalCount == 0) return Ok(new { total_count = 0, items = Array.Empty<object>() });
        int filterCityId = req.CityId ?? 0;

        var resultList = await query
            .OrderByDescending(s => s.ExperienceYears)
            .Skip(skip).Take(take)
            .Select(s => new
            {
                uid = s.ServiceId,
                nick = s.StageName ?? ($"{s.Performer.User.FirstName} {s.Performer.User.LastName}"),
                phot = s.Performer.User.PhotoUrl,
                rtg = s.Rating,
                cat = s.CategoryId,
                cost = s.PriceHour,
                curr = s.Performer.User.CurrencyCode,
                is_cc = (filterCityId > 0 && s.Performer.CityId != filterCityId) ? (byte)1 : (byte)0
            })
            .ToListAsync();

                x.PricePacks,
                x.Performer.LangCommCodes,
                x.LangCondCodes,
                EventTypeIds = x.ServiceTypeCodes.Select(t => t.TypeCodeId).ToList()
            })
            .FirstOrDefaultAsync();

        if (s == null) return NotFound();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        int calculatedAge = s.Birthday.HasValue ? today.Year - s.Birthday.Value.Year - (s.Birthday.Value > today.AddYears(-(today.Year - s.Birthday.Value.Year)) ? 1 : 0) : 0;
        var levelInfo = LevelHelper.Calculate(s.Points);

        List<MediaItemDto> MapMedia(IEnumerable<MediaItem>? items) =>
            items?.Select(m => new MediaItemDto { Url = m.Url, Title = m.Title ?? "" }).ToList() ?? new();

        return Ok(new PerformerFullProfileDto
        {
            ServiceId = s.ServiceId,
            Rating = s.Rating,
            Level = levelInfo.Level,
            PointsPercentage = levelInfo.Percentage,
}
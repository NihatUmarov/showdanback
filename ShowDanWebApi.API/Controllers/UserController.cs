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
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == CurrentUserId);
        if (user == null) return NotFound();

        var payload = string.IsNullOrWhiteSpace(request.PayloadJson)
            ? null
            : JsonSerializer.Deserialize<UserPersonalInfoDto>(request.PayloadJson, JsonConfig.Options);

        if (payload == null && request.Avatar == null)
        {
            return Ok(new UserPersonalInfoDto
            {
                Phone = user.Phone,
                FirstName = user.FirstName,
                LastName = user.LastName,
                GenderCode = user.GenderCode,
                Birthday = user.Birthday,
                AvatarUrl = user.PhotoUrl
            });
        }

        if (payload != null)
        {
            if (payload.Phone != null) user.Phone = payload.Phone;
            if (payload.FirstName != null) user.FirstName = payload.FirstName;
            if (payload.LastName != null) user.LastName = payload.LastName;
            if (payload.GenderCode.HasValue) user.GenderCode = payload.GenderCode;
            if (payload.Birthday.HasValue) user.Birthday = payload.Birthday;
        }

        if (request.Avatar != null)
        {
            var filesToDelete = new List<string?>();
            if (!string.IsNullOrEmpty(user.PhotoUrl)) filesToDelete.Add(user.PhotoUrl);
            if (!string.IsNullOrEmpty(user.PhotoThumbUrl)) filesToDelete.Add(user.PhotoThumbUrl);

            if (filesToDelete.Count > 0) await _imageService.DeleteMediaAsync(filesToDelete);

            var (fullUrl, thumbUrl) = await _imageService.ProcessAndSaveAvatarAsync(request.Avatar);
            user.PhotoUrl = fullUrl;
            user.PhotoThumbUrl = thumbUrl;
        }

        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userData = await _context.Users
            .AsNoTracking()
            .Where(u => u.UserId == CurrentUserId)
            .Select(u => new
            {
                u.UserId,
                u.FirstName,
                u.LastName,
                u.Balance,
                u.CurrencyCode,
                u.PhotoThumbUrl,
                Services = u.PerformerProfile != null
                    ? u.PerformerProfile.PerformerServices.Select(s => new { s.ServiceId, s.StageName, s.CategoryId }).ToList()
                    : null
            })
            .FirstOrDefaultAsync();

        if (userData == null) return NotFound();

        string clientName = !string.IsNullOrWhiteSpace(userData.FirstName) ? userData.FirstName : "Аноним";

        var userDto = new UserDTO
        {
            UserId = userData.UserId.ToString(),
            FirstName = userData.FirstName,
            LastName = userData.LastName,
            Balance = userData.Balance.ToString(),
            Currency = userData.CurrencyCode,
            AvatarUrl = userData.PhotoThumbUrl,
            CurrentRole = CurrentUserRole,
            Profiles = new List<ProfileSwitchItemDto>
            {
                new ProfileSwitchItemDto { Role = "c", Title = clientName, CategoryId = 0 }
            }
        };

        if (userData.Services != null)
        {
            userDto.Profiles.AddRange(userData.Services.Select(s => new ProfileSwitchItemDto
            {
                Role = "p",
                TargetServiceId = s.ServiceId,
                Title = !string.IsNullOrWhiteSpace(s.StageName) ? s.StageName : clientName,
                CategoryId = s.CategoryId
            }));
        }

        return Ok(userDto);
    }

    [LocalizationRequired]
    [HttpPost("search")]
    public async Task<IActionResult> SearchPerformers([FromBody] PerformersFilterDto req)
    {
        var query = _context.PerformerServices.AsNoTracking();

        if (req.DirId > 0) query = query.Where(s => s.Category.DirectionId == req.DirId);
        if (req.CatIds?.Count > 0) query = query.Where(s => req.CatIds.Contains(s.CategoryId));
        if (req.GenderCode != null) query = query.Where(s => s.Performer.User.GenderCode == req.GenderCode);
        if (req.MinCost > 0) query = query.Where(s => s.PriceHour >= req.MinCost);
        if (req.MaxCost > 0) query = query.Where(s => s.PriceHour <= req.MaxCost);
        if (req.LangCode?.Count > 0) query = query.Where(s => EF.Functions.JsonContains(s.LangCondCodes, req.LangCode));

        var (skip, take) = GetPagination(req.Start, req.End, maxPageSize: 100, defaultPageSize: 10);
        var totalCount = await query.CountAsync();

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

        return Ok(new { total_count = totalCount, items = resultList });
    }

    [LocalizationRequired]
    [HttpPost("details")]
    public async Task<IActionResult> GetPerformerDetails([FromBody] PerformerIdDto request)
    {
        var s = await _context.PerformerServices
            .AsNoTracking()
            .Where(x => x.ServiceId == request.ServiceId)
            .Select(x => new
            {
                x.ServiceId,
                x.Rating,
                x.Description,
                x.StageName,
                x.Performer.User.FirstName,
                x.Performer.User.LastName,
                x.Performer.User.PhotoUrl,
                x.Performer.User.Birthday,
                x.Performer.User.GenderCode,
                x.ExperienceYears,
                x.Performer.User.Points,
                x.CategoryId,
                x.WorkStyle,
                x.WorkStyleOptionally,
                x.PriceHour,
                x.Performer.User.CurrencyCode,
                x.PhotosPersonal,
                x.PhotosLive,
                x.AudiosPersonal,
                x.VideoPersonal,
                x.Performer.Socials,
                Loc = x.Performer.Location,
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
            GenderCode = s.GenderCode,
            DisplayName = s.StageName ?? $"{s.FirstName} {s.LastName}",
            AvatarUrl = s.PhotoUrl,
            Age = calculatedAge,
            Description = s.Description?.Get(CurrentLang) ?? string.Empty,
            ExperienceYears = s.ExperienceYears,
            CategoryId = s.CategoryId,
            WorkStyle = s.WorkStyle,
            WorkStyleOptionally = s.WorkStyleOptionally ?? 0,
            Currency = s.CurrencyCode,
            PersonalPhotos = MapMedia(s.PhotosPersonal),
            LivePhotos = MapMedia(s.PhotosLive),
            Audios = MapMedia(s.AudiosPersonal),
            Videos = MapMedia(s.VideoPersonal),
            Socials = s.Socials ?? new Dictionary<string, string>(),
            Latitude = s.Loc?.Y,
            Longitude = s.Loc?.X,
            PricePacks = s.PricePacks ?? new List<PricePackage>(),
            EventTypes = s.EventTypeIds ?? new List<int>(),
            LangCommCodes = s.LangCommCodes ?? new List<string>(),
            LangCondCodes = s.LangCondCodes ?? new List<string>(),
        });
    }
}
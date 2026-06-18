using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using ShowDanWebApi.API.Service;
using ShowDanWebApi.Core.DTO;
using ShowDanWebApi.Core.Entities.Users;
using ShowDanWebApi.Core.Language;
using ShowDanWebApi.Data;
using System.Text.Json;

namespace ShowDanWebApi.API.Controllers;

[Authorize]
[Route("api/performers")]
public class PerformersController : BaseController
{
    private readonly AppDbContext _context;
    private readonly IImageProcessingService _imageService;
    private readonly ITranslationService _translationService;
    private readonly IMapService _mapService;

    public PerformersController(AppDbContext context, IImageProcessingService imageService, ITranslationService translationService, IMapService mapService)
    {
        _context = context;
        _imageService = imageService;
        _translationService = translationService;
        _mapService = mapService;
    }

    [LocalizationRequired]
    [HttpGet("get")]
    public async Task<IActionResult> GetManageProfile()
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.PerformerProfile)
                .ThenInclude(p => p!.PerformerServices)
                    .ThenInclude(s => s.ServiceGenreCodes)
            .Include(u => u.PerformerProfile)
                .ThenInclude(p => p!.PerformerServices)
                    .ThenInclude(s => s.ServiceTypeCodes)
            .Include(u => u.PerformerProfile)
                .ThenInclude(p => p!.PerformerServices)
                    .ThenInclude(s => s.ServiceExtraCodes)
            .FirstOrDefaultAsync(u => u.UserId == CurrentUserId);

        if (user == null) return NotFound();

        var dto = new PerformerManageResponseDto
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Phone = user.Phone,
            Birthday = user.Birthday,
            GenderCode = user.GenderCode,
            AvatarUrl = user.PhotoUrl,
            CurrencyCode = user.CurrencyCode ?? "USD"
        };

        var performer = user.PerformerProfile;
        if (performer == null) return Ok(dto);

        dto.Latitude = performer.Location?.Y;
        dto.Longitude = performer.Location?.X;
        dto.CityId = performer.CityId;
        dto.CommunicationLanguages = performer.LangCommCodes ?? new();

        var service = CurrentServiceId.HasValue
            ? performer.PerformerServices.FirstOrDefault(s => s.ServiceId == CurrentServiceId.Value)
            : null;

        if (service == null) return Ok(dto);

        dto.CategoryId = service.CategoryId;
        dto.Nickname = service.StageName;
        dto.ExperienceYears = service.ExperienceYears;
        dto.Description = service.Description?.Get(CurrentLang) ?? "";
        dto.WorkStyle = service.WorkStyle;
        dto.WorkStyleOptionally = service.WorkStyleOptionally;
        dto.ParameterRange = service.ParameterRange;
        dto.PriceHours = service.PriceHour;
        dto.PricePackages = service.PricePacks ?? new();
        dto.WorkLanguages = service.LangCondCodes ?? new();

        dto.ServiceTypeCodes = service.ServiceTypeCodes.Select(t => t.TypeCodeId).ToList();
        dto.ServiceGenreCodes = service.ServiceGenreCodes.Select(g => g.GenreCodeId).ToList();
        dto.ServiceExtraCodes = service.ServiceExtraCodes.Select(e => e.ExtraCodeId).ToList();

        dto.PersonalPhotos = MapMedia(service.PhotosPersonal);
        dto.LivePhotos = MapMedia(service.PhotosLive);
        dto.Videos = MapMedia(service.VideoPersonal);
        dto.Audios = MapMedia(service.AudiosPersonal);

        return Ok(dto);
    }

    [LocalizationRequired]
    [HttpPost("manage")]
    public async Task<IActionResult> SaveProfile([FromForm] SavePerformerProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PayloadJson)) return BadRequest("Payload is empty");

        var payload = JsonSerializer.Deserialize<PerformerProfilePayload>(request.PayloadJson, JsonConfig.Options);
        if (payload == null) return BadRequest("Invalid JSON payload");

        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync<IActionResult>(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            var user = await _context.Users
                .Include(u => u.PerformerProfile)
                    .ThenInclude(p => p!.PerformerServices)
                        .ThenInclude(s => s.ServiceGenreCodes)
                .Include(u => u.PerformerProfile)
                    .ThenInclude(p => p!.PerformerServices)
                        .ThenInclude(s => s.ServiceTypeCodes)
                .Include(u => u.PerformerProfile)
                    .ThenInclude(p => p!.PerformerServices)
                        .ThenInclude(s => s.ServiceExtraCodes)
                .FirstOrDefaultAsync(u => u.UserId == CurrentUserId);

            if (user == null) return NotFound("User not found");
            var performer = user.PerformerProfile ??= new Performers { UserId = CurrentUserId, CityId = payload.CityId ?? 1 };
            if (_context.Entry(performer).State == EntityState.Detached) _context.Performers.Add(performer);

            var service = CurrentServiceId.HasValue
                ? performer.PerformerServices.FirstOrDefault(s => s.ServiceId == CurrentServiceId.Value)
                : performer.PerformerServices.FirstOrDefault(s => s.CategoryId == payload.CategoryId);

            if (service == null)
            {
                service = new PerformerServices { PerformerId = performer.UserId, CategoryId = payload.CategoryId };
                performer.PerformerServices.Add(service);
                _context.PerformerServices.Add(service);
            }

            UpdateBaseFields(user, performer, service, payload);
            await UpdateLocationAsync(performer, payload);

            var (textsToTranslate, descriptionChanged) = CollectTexts(request, payload, service);
            var translations = textsToTranslate.Count > 0
                ? await _translationService.TranslateBatchAsync(textsToTranslate, CurrentLang)
                : new Dictionary<string, MultiLang>();

            if (descriptionChanged && translations.TryGetValue("desc", out var descMulti))
                service.Description = descMulti;

            await ProcessDeletionsAsync(service, payload, user, request.Avatar != null);

            if (request.Avatar != null)
                (user.PhotoUrl, user.PhotoThumbUrl) = await _imageService.ProcessAndSaveAvatarAsync(request.Avatar);

            service.PhotosPersonal ??= new List<MediaItem>();
            service.PhotosLive ??= new List<MediaItem>();
            service.VideoPersonal ??= new List<MediaItem>();
            service.AudiosPersonal ??= new List<MediaItem>();

            await ProcessAndAppendUploadsAsync(request.NewPersonalPhotos, payload.NewPersonalPhotoTitles, "p_photo", translations, service.PhotosPersonal, 2);
            await ProcessAndAppendUploadsAsync(request.NewLivePhotos, payload.NewLivePhotoTitles, "l_photo", translations, service.PhotosLive, 2);
            await ProcessAndAppendUploadsAsync(request.NewAudioFiles, payload.NewAudioTitles, "audio", translations, service.AudiosPersonal, 3);

            if (payload.Videos != null && payload.Videos.Count > 0)
            {
                service.VideoPersonal.AddRange(payload.Videos.Select(v => new MediaItem
                {
                    Url = v.Url,
                    Title = v.Title
                }));
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { id = service.ServiceId });
        });
    }

    private List<MediaItemDto> MapMedia(IEnumerable<MediaItem>? items) =>
        items?.Select(m => new MediaItemDto { Url = m.Url, Title = m.Title ?? "" }).ToList() ?? new();

    private void UpdateBaseFields(Users user, Performers performer, PerformerServices service, PerformerProfilePayload payload)
    {
        if (payload.Name != null) user.FirstName = payload.Name;
        if (payload.LastName != null) user.LastName = payload.LastName;
        if (payload.Phone != null) user.Phone = payload.Phone;
        if (payload.GenderCode.HasValue) user.GenderCode = payload.GenderCode.Value;
        if (payload.Birthday.HasValue) user.Birthday = payload.Birthday.Value;
        if (payload.CurrencyId != null) user.CurrencyCode = payload.CurrencyId.ToUpper();
        if (payload.CityId.HasValue) performer.CityId = payload.CityId.Value;
        if (payload.CommunicationLanguages != null) performer.LangCommCodes = new List<string>(payload.CommunicationLanguages);
        if (payload.WorkLanguages != null) service.LangCondCodes = new List<string>(payload.WorkLanguages);
        if (payload.Nickname != null) service.StageName = payload.Nickname;
        if (payload.ExperienceYears.HasValue) service.ExperienceYears = payload.ExperienceYears.Value;
        if (payload.PriceHours != null) service.PriceHour = payload.PriceHours;
        if (payload.PricePackages != null) service.PricePacks = new List<PricePackage>(payload.PricePackages);
        if (payload.ParameterRange != null) service.ParameterRange = payload.ParameterRange;

        service.WorkStyle = payload.WorkStyle;
        service.WorkStyleOptionally = payload.WorkStyleOptionally;

        service.ServiceTypeCodes.Clear();
        if (payload.ServiceTypeCodes != null)
        {
            foreach (var id in payload.ServiceTypeCodes)
                service.ServiceTypeCodes.Add(new ServiceTypeCodes { ServiceId = service.ServiceId, TypeCodeId = id });
        }

        service.ServiceGenreCodes.Clear();
        if (payload.ServiceGenreCodes != null)
        {
            foreach (var id in payload.ServiceGenreCodes)
                service.ServiceGenreCodes.Add(new ServiceGenreCodes { ServiceId = service.ServiceId, GenreCodeId = id });
        }

        service.ServiceExtraCodes.Clear();
        if (payload.ServiceExtraCodes != null)
        {
            foreach (var id in payload.ServiceExtraCodes)
                service.ServiceExtraCodes.Add(new ServiceExtraCodes { ServiceId = service.ServiceId, ExtraCodeId = id });
        }
    }

    private async Task UpdateLocationAsync(Performers performer, PerformerProfilePayload payload)
    {
        if (!payload.Latitude.HasValue || !payload.Longitude.HasValue) return;

        performer.Location = new Point(payload.Longitude.Value, payload.Latitude.Value) { SRID = 4326 };

        int detectedCityId = await _mapService.IdentifyCityIdAsync(_context, payload.Latitude.Value, payload.Longitude.Value);
        if (detectedCityId > 0) performer.CityId = detectedCityId;
    }

    private (Dictionary<string, string> texts, bool descChanged) CollectTexts(SavePerformerProfileRequest req, PerformerProfilePayload payload, PerformerServices service)
    {
        var texts = new Dictionary<string, string>();
        bool descChanged = false;

        if (!string.IsNullOrWhiteSpace(payload.Description) && payload.Description.Trim() != (service.Description?.Get(CurrentLang) ?? ""))
        {
            texts["desc"] = payload.Description;
            descChanged = true;
        }

        AddTitlesToTranslate(texts, payload.NewPersonalPhotoTitles, "p_photo");
        AddTitlesToTranslate(texts, payload.NewLivePhotoTitles, "l_photo");
        AddTitlesToTranslate(texts, payload.NewAudioTitles, "audio");

        return (texts, descChanged);
    }

    private void AddTitlesToTranslate(Dictionary<string, string> texts, List<string>? titles, string prefix)
    {
        if (titles == null) return;
        for (int i = 0; i < titles.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(titles[i])) texts[$"{prefix}_{i}"] = titles[i];
        }
    }

    private async Task ProcessDeletionsAsync(PerformerServices service, PerformerProfilePayload payload, Users user, bool hasNewAvatar)
    {
        var filesToDelete = new List<string>();

        if (payload.DeletePhotoUrls?.Count > 0)
        {
            filesToDelete.AddRange(service.PhotosPersonal!.Where(p => payload.DeletePhotoUrls.Contains(p.Url)).Select(p => p.Url));
            filesToDelete.AddRange(service.PhotosLive!.Where(p => payload.DeletePhotoUrls.Contains(p.Url)).Select(p => p.Url));
            service.PhotosPersonal!.RemoveAll(p => payload.DeletePhotoUrls.Contains(p.Url));
            service.PhotosLive!.RemoveAll(p => payload.DeletePhotoUrls.Contains(p.Url));
        }

        if (payload.DeleteAudioUrls?.Count > 0)
        {
            filesToDelete.AddRange(service.AudiosPersonal!.Where(a => payload.DeleteAudioUrls.Contains(a.Url)).Select(a => a.Url));
            service.AudiosPersonal!.RemoveAll(a => payload.DeleteAudioUrls.Contains(a.Url));
        }

        if (payload.DeleteVideoUrls?.Count > 0)
        {
            service.VideoPersonal!.RemoveAll(v => payload.DeleteVideoUrls.Contains(v.Url));
        }

        if (hasNewAvatar && !string.IsNullOrEmpty(user.PhotoUrl))
        {
            filesToDelete.Add(user.PhotoUrl);
            filesToDelete.Add(user.PhotoThumbUrl!);
        }

        if (filesToDelete.Count > 0) await _imageService.DeleteMediaAsync(filesToDelete);
    }

    private async Task ProcessAndAppendUploadsAsync(List<IFormFile>? files, List<string>? titles, string prefix, Dictionary<string, MultiLang> translations, List<MediaItem> targetList, int mediaType)
    {
        if (files == null || files.Count == 0) return;

        for (int i = 0; i < files.Count; i++)
        {
            var url = await _imageService.ProcessAndSaveMediaAsync(files[i], mediaType);
            var rawTitle = titles?.ElementAtOrDefault(i) ?? Path.GetFileNameWithoutExtension(files[i].FileName);
            if (string.IsNullOrWhiteSpace(rawTitle)) rawTitle = "Media";

            targetList.Add(new MediaItem { Url = url, Title = rawTitle });
        }
    }
}
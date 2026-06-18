using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShowDanWebApi.Core.Entities.News;
using ShowDanWebApi.Data;
using ShowDanWebApi.API.Service;
using System.Text.Json.Serialization;

namespace ShowDanWebApi.API.Controllers;

[Authorize]
[Route("api/news")]
public class NewsController : BaseController
{
    private readonly AppDbContext _context;
    private readonly ITranslationService _translationService;
    private readonly IImageProcessingService _imageService;

    public NewsController(AppDbContext context, ITranslationService translationService, IImageProcessingService imageService)
    {
        _context = context;
        _translationService = translationService;
        _imageService = imageService;
    }

    [LocalizationRequired]
    [HttpPost("list")]
    public async Task<IActionResult> GetNewsList([FromBody] NewsListRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.GeoCode)) return BadRequest();

        var items = await _context.News
            .AsNoTracking()
            .Where(n => n.CountriesCode == req.GeoCode.ToLower().Trim()
                        && n.IsApproved
                        && !n.IsArchived)
            .OrderByDescending(n => n.ApprovedAt)
            .Select(n => new
            {
                id = n.NewsId,
                ttl = n.Title != null ? n.Title.Get(CurrentLang) : string.Empty,
                pht = n.PhotoUrls != null && n.PhotoUrls.Length > 0 ? n.PhotoUrls[0] : null,
                ts = n.ApprovedAt,
                sz = n.Size,
                lk = _context.NewsLikes.Any(l => l.UserId == CurrentUserId && l.NewsId == n.NewsId)
            })
            .ToListAsync();

        return Ok(items);
    }

    [LocalizationRequired]
    [HttpPost("details")]
    public async Task<IActionResult> GetNewsFull([FromBody] NewsDetailsRequest req)
    {
        var news = await _context.News.AsNoTracking().FirstOrDefaultAsync(n => n.NewsId == req.NewsId);
        if (news == null) return NotFound();

        var totalCommentsCount = await _context.NewsComments.CountAsync(c => c.NewsId == req.NewsId);

        var top3Comments = await _context.NewsComments
            .AsNoTracking()
            .Where(c => c.NewsId == req.NewsId)
            .Join(_context.Users,
                comment => comment.UserId,
                user => user.UserId,
                (comment, user) => new
                {
                    id = comment.NewsCommentId,
                    cmm = comment.Comment ?? "",
                    ts = comment.TS,
                    usr = user.FirstName ?? "Anonim",
                    pht = user.PhotoUrl
                })
            .OrderByDescending(c => c.ts)
            .Take(3)
            .ToListAsync();

        var recommendationsList = await _context.News
            .AsNoTracking()
            .Where(n => n.NewsId != req.NewsId && n.CountriesCode == news.CountriesCode && n.IsApproved && !n.IsArchived)
            .OrderByDescending(n => n.ApprovedAt)
            .Take(4)
            .Select(r => new
            {
                id = r.NewsId,
                ttl = r.Title != null ? r.Title.Get(CurrentLang) : string.Empty,
                pht = r.PhotoUrls != null && r.PhotoUrls.Length > 0 ? r.PhotoUrls[0] : null, // Поджали ключ с phts до pht для унификации
                ts = r.ApprovedAt,
                sz = r.Size,
                lk = _context.NewsLikes.Any(l => l.UserId == CurrentUserId && l.NewsId == r.NewsId)
            })
            .ToListAsync();

        return Ok(new
        {
            id = news.NewsId,
            ttl = news.Title?.Get(CurrentLang) ?? string.Empty,
            dsn = news.Description?.Get(CurrentLang) ?? string.Empty,
            cnt = news.Content?.Get(CurrentLang) ?? string.Empty,
            phs = news.PhotoUrls ?? Array.Empty<string>(),
            vd = news.VideoUrl,
            ts = news.ApprovedAt,
            sz = news.Size,
            cmm = top3Comments,
            m_cmm = totalCommentsCount > 3, // Сократили has_more_cmm -> m_cmm
            t_cmm = totalCommentsCount,     // Сократили total_cmm -> t_cmm
            rec_l = recommendationsList
        });
    }

    [HttpPost("comments-list")]
    public async Task<IActionResult> GetNewsCommentsList([FromBody] NewsCommentsListRequest req)
    {
        var (skip, take) = GetPagination(req.Start, req.End, maxPageSize: 20, defaultPageSize: 20);

        var comments = await _context.NewsComments
            .AsNoTracking()
            .Where(c => c.NewsId == req.NewsId)
            .Join(_context.Users,
                comment => comment.UserId,
                user => user.UserId,
                (comment, user) => new
                {
                    id = comment.NewsCommentId,
                    cmm = comment.Comment ?? "",
                    ts = comment.TS,
                    usr = user.FirstName ?? "Anonim",
                    pht = user.PhotoUrl
                })
            .OrderByDescending(c => c.ts)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return Ok(comments);
    }

    [HttpPost("like")]
    public async Task<IActionResult> ToggleLike([FromBody] LikeRequest req)
    {
        var existingLike = await _context.NewsLikes
            .FirstOrDefaultAsync(l => l.NewsId == req.NewsId && l.UserId == CurrentUserId);

        if (existingLike != null)
        {
            _context.NewsLikes.Remove(existingLike);
            await _context.SaveChangesAsync();
            return Ok(new { lk = false }); // Сократили с liked -> lk
        }

        _context.NewsLikes.Add(new NewsLike { NewsId = req.NewsId, UserId = CurrentUserId });
        await _context.SaveChangesAsync();
        return Ok(new { lk = true }); // Сократили с liked -> lk
    }

    [HttpPost("comment")]
    public async Task<IActionResult> AddComment([FromBody] AddCommentRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Text)) return BadRequest();

        var newsExists = await _context.News.AnyAsync(n => n.NewsId == req.NewsId);
        if (!newsExists) return NotFound();

        var newComment = new NewsComment { NewsId = req.NewsId, UserId = CurrentUserId, Comment = req.Text, TS = DateTime.UtcNow };
        _context.NewsComments.Add(newComment);
        await _context.SaveChangesAsync();

        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.UserId == CurrentUserId)
            .Select(u => new { u.FirstName, u.PhotoUrl })
            .FirstOrDefaultAsync();

        return Ok(new
        {
            id = newComment.NewsCommentId,
            cmm = newComment.Comment ?? "",
            ts = newComment.TS,
            usr = user?.FirstName ?? "Anonim",
            pht = user?.PhotoUrl
        });
    }

    #region Admin Endpoints (Создание, Просмотр и Модерация)

    [LocalizationRequired]
    [HttpPost("admin-list")]
    public async Task<IActionResult> GetAdminNewsList([FromBody] AdminNewsListRequest req)
    {
        var (skip, take) = GetPagination(req.Start, req.End, maxPageSize: 50, defaultPageSize: 20);

        var query = _context.News.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(req.GeoCode))
        {
            query = query.Where(n => n.CountriesCode == req.GeoCode.ToLower().Trim());
        }

        var items = await query
            .OrderByDescending(n => n.TS)
            .Skip(skip)
            .Take(take)
            .Select(n => new
            {
                id = n.NewsId,
                ttl = n.Title != null ? n.Title.Get(CurrentLang) : string.Empty,
                pht = n.PhotoUrls != null && n.PhotoUrls.Length > 0 ? n.PhotoUrls[0] : null,
                ts = n.TS,
                sz = n.Size,
                g_cd = n.CountriesCode,
                app = n.IsApproved,
                arc = n.IsArchived
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateNews([FromForm] CreateNewsFormRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.PayloadJson))
            return BadRequest(new { error = "payload_json_required" });

        CreateNewsJsonPayload? payload;
        try
        {
            payload = System.Text.Json.JsonSerializer.Deserialize<CreateNewsJsonPayload>(req.PayloadJson);
            if (payload == null) return BadRequest(new { error = "invalid_json_format" });
        }
        catch (Exception)
        {
            return BadRequest(new { error = "json_parsing_failed" });
        }

        if (string.IsNullOrWhiteSpace(payload.TitleOriginal) || string.IsNullOrWhiteSpace(payload.GeoCode))
            return BadRequest(new { error = "missing_required_fields" });

        var savedUrls = new List<string>();
        if (req.Photos != null && req.Photos.Count > 0)
        {
            foreach (var file in req.Photos)
            {
                if (file.Length > 0)
                {
                    string fileUrl = await _imageService.ProcessAndSaveMediaAsync(file, 4);
                    savedUrls.Add(fileUrl);
                }
            }
        }

        var translatedTitle = await _translationService.TranslateToAllAsync(payload.TitleOriginal, payload.LangCode);
        var translatedDesc = await _translationService.TranslateToAllAsync(payload.DescOriginal ?? "", payload.LangCode);
        var translatedContent = await _translationService.TranslateToAllAsync(payload.ContentOriginal ?? "", payload.LangCode);

        var news = new News
        {
            Title = translatedTitle,
            Description = translatedDesc,
            Content = translatedContent,
            PhotoUrls = savedUrls.ToArray(),
            VideoUrl = payload.VideoUrl,
            Size = payload.Size,
            CountriesCode = payload.GeoCode.ToLower().Trim(),
            IsApproved = false,
            IsArchived = false,
            TS = DateTime.UtcNow
        };

        _context.News.Add(news);
        await _context.SaveChangesAsync();

        return Ok(new { id = news.NewsId, status = "created_awaiting_approval" });
    }

    [HttpPost("moderate")]
    public async Task<IActionResult> ModerateNews([FromBody] ModerateNewsRequest req)
    {
        var news = await _context.News.FirstOrDefaultAsync(n => n.NewsId == req.NewsId);
        if (news == null) return NotFound();

        switch (req.Action.ToLower().Trim())
        {
            case "approve":
                news.IsApproved = true;
                news.IsArchived = false;
                news.ApprovedAt = DateTime.UtcNow;
                break;

            case "archive":
                news.IsArchived = true;
                break;

            case "delete":
                if (news.PhotoUrls != null && news.PhotoUrls.Length > 0)
                {
                    await _imageService.DeleteMediaAsync(news.PhotoUrls);
                }
                _context.News.Remove(news);
                break;

            default:
                return BadRequest(new { error = "unknown_action" });
        }

        await _context.SaveChangesAsync();
        return Ok(new { id = req.NewsId, status = req.Action });
    }

    #endregion
}

// --- DTO Records ---

public record NewsListRequest(
    [property: JsonPropertyName("g_cd")] string GeoCode
);

public record AdminNewsListRequest(
    [property: JsonPropertyName("s_l")] int Start,
    [property: JsonPropertyName("e_l")] int End,
    [property: JsonPropertyName("g_cd")] string? GeoCode
);

public record NewsDetailsRequest(
    [property: JsonPropertyName("id")] int NewsId
);

public record NewsCommentsListRequest(
    [property: JsonPropertyName("id")] int NewsId,
    [property: JsonPropertyName("s_l")] int Start,
    [property: JsonPropertyName("e_l")] int End
);

public record LikeRequest(
    [property: JsonPropertyName("id")] int NewsId
);

public record AddCommentRequest(
    [property: JsonPropertyName("id")] int NewsId,
    [property: JsonPropertyName("txt")] string Text
);

public class CreateNewsFormRequest
{
    [FromForm(Name = "pj")] public string PayloadJson { get; set; } = string.Empty;
    [FromForm(Name = "phs")] public List<IFormFile>? Photos { get; set; }
}

public record CreateNewsJsonPayload(
    [property: JsonPropertyName("ttl")] string TitleOriginal,
    [property: JsonPropertyName("desc")] string? DescOriginal,
    [property: JsonPropertyName("cnt")] string? ContentOriginal,
    [property: JsonPropertyName("l_cd")] string LangCode,
    [property: JsonPropertyName("g_cd")] string GeoCode,
    [property: JsonPropertyName("sz")] int Size,
    [property: JsonPropertyName("vd")] string? VideoUrl
);

public record ModerateNewsRequest(
    [property: JsonPropertyName("id")] int NewsId,
    [property: JsonPropertyName("act")] string Action
);
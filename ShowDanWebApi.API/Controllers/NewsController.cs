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
            .Where(c => c.NewsId == req.NewsI
        var recommendationsList = await _context.News
            .AsNoTracking()
            .Where(n => n.NewsId != req.NewsId && n.CountriesCode == news.CountriesCode && n.IsApproved && !n.IsArchived)
            .OrderByDescending(n => n.ApprovedAt)
            .Take(4)
            .Select(r => new
            {
            m_cmm = totalCommentsCount > 3, // Сократили has_more_cmm -> m_cmm
            t_cmm = totalCommentsCount,     // Сократили total_cmm -> t_cmm
            rec_l = recommendationsList
        });
    }

    [HttpPost("comment")]
    public async Task<IActionResult> AddC
    }

    #region Admin Endpoints (Создание, Просмотр и Модерация)

    [LocalizationRequired]
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateNews([FromForm] CreateNewsFormRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.PayloadJson))
            return BadRequest(new { error = "payload_json_required" });

        CreateNewsJsonPayload? paylo
                {
                    string fileUrl = await _imageService.ProcessAndSaveMediaAsync(file, 4);
                    savedUrls.Add(fileUrl);
                }
            }

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
    public

    #endregion
}

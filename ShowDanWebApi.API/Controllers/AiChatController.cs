using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ShowDanWebApi.API.Service;
using ShowDanWebApi.Core.DTO;
using ShowDanWebApi.Data;

namespace ShowDanWebApi.API.Controllers;

[Authorize]
[ApiController]
[Route("api/ai")]
public class AiChatController : BaseController
{

        if (string.IsNullOrWhiteSpace(aiReply) || aiReply.Length < 4 || aiReply.Contains("запутался"))
        {
            aiReply = "Класс! 🔥 Кого именно из артистов и в каком городе мы ищем?";
        }

        aiState.History.Add(new ChatHistoryMessage { Role = "assistant", Content = aiReply });
        if (aiState.History.Count > 6)
        {
            aiState.History = aiState.History.Skip(aiState.History.Count - 6).ToList();
        }

        var cacheOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(10));
        _cache.Set(cacheKey, aiState, cacheOptions);

        if (!isReady)
        {
            return Ok(new { status = "clarification", message = aiReply, items = Array.Empty<object>() });
        }
        var query = _context.PerformerServices.AsNoTracking();

        if (isCatReady)
            query = query.Where(s => s.CategoryId == aiState.Slots.CategoryId!.Value);

        if (isCityReady)
            query = query.Where(s => s.Performer.CityId == aiState.Slots.CityId!.Value);

        if (aiState.Slots.Budget is > 0)
            query = query.Where(s => s.PriceHour <= aiState.Slots.Budget.Value);

        if (aiState.Slots.Gender != null)
        {
            char gCode = (char)aiState.Slots.Gender;
            query = query.Where(s => s.Performer.User.GenderCode == gCode);
        }
}
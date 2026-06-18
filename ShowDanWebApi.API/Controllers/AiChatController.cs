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
    private readonly AppDbContext _context;
    private readonly IAiAssistantService _aiService;
    private readonly IMemoryCache _cache;

    private static readonly HashSet<string> Greetings = new([
        "привет", "здранствуйте", "здравствуйте", "салом", "hi", "hello", "assalomu alaykum", "salom"
    ]);

    public AiChatController(AppDbContext context, IAiAssistantService aiService, IMemoryCache cache) =>
        (_context, _aiService, _cache) = (context, aiService, cache);

    [HttpPost("chat")]
    public async Task<IActionResult> ChatWithAi([FromBody] AiChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest("Сообщение не может быть пустым");

        var cleanMessage = request.Message.ToLower().Trim(' ', ',', '.', '!', '?', '-');
        string cacheKey = $"ai_state_{CurrentUserId}";

        if (Greetings.Contains(cleanMessage))
        {
            return Ok(new
            {
                status = "clarification",
                message = "Салют! ✨ Я твой праздничный ИИ-продюсер Shoui! Сделаем шоу века! Кого и в каком городе ищем? 🔥",
                items = Array.Empty<object>()
            });
        }

        if (!_cache.TryGetValue(cacheKey, out UserAiState? aiState) || aiState == null)
        {
            aiState = new UserAiState();
        }

        var detectedCat = AiSearchValidator.NormalizeCategory(request.Message);
        if (detectedCat.HasValue && detectedCat.Value > 0) aiState.Slots.CategoryId = detectedCat.Value;

        var detectedCity = AiSearchValidator.NormalizeCity(request.Message);
        if (detectedCity.HasValue && detectedCity.Value > 0) aiState.Slots.CityId = detectedCity.Value;

        var detectedEvent = AiSearchValidator.NormalizeEventType(request.Message);
        if (!string.IsNullOrWhiteSpace(detectedEvent)) aiState.Slots.Event = detectedEvent;

        var detectedGender = AiSearchValidator.NormalizeGender(request.Message);
        if (detectedGender.HasValue) aiState.Slots.Gender = detectedGender.Value;

        if (System.Text.RegularExpressions.Regex.Match(request.Message, @"\d+") is { Success: true } match &&
            int.TryParse(match.Value, out int budget) && budget is > 0 and < 100000)
        {
            aiState.Slots.Budget = budget;
        }

        aiState.History.Add(new ChatHistoryMessage { Role = "user", Content = request.Message });

        bool isCatReady = aiState.Slots.CategoryId.HasValue && aiState.Slots.CategoryId.Value > 0;
        bool isCityReady = aiState.Slots.CityId.HasValue && aiState.Slots.CityId.Value > 0;
        bool isReady = isCatReady && isCityReady;
        string aiReply;

        try
        {
            aiReply = await _aiService.ProcessChatAsync(request.Message, aiState);
        }
        catch
        {
            aiReply = (!isCatReady, !isCityReady) switch
            {
                (false, true) => "Выбор шикарный! 🏙️ А в каком городе будет мероприятие?",
                (true, false) => "Локация бомба! ✨ А кто именно нужен? Ведущий, DJ или, может, кавер-группа?",
                _ => "Нереально! 🎉 Расскажи скорее подробности, кто нужен и где празднуем?"
            };
        }

        if (aiReply.Contains("[SYSTEM_COMMAND: CLEAR_HISTORY]"))
        {
            _cache.Remove(cacheKey);
            return Ok(new
            {
                status = "cleared",
                message = "История чата успешно сброшена! Начнем сначала? Кто нам нужен и в каком городе? 🔄",
                items = Array.Empty<object>()
            });
        }

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
        var resultList = await query
            .OrderByDescending(s => s.Rating)
            .Take(10)
            .Select(s => new
            {
                uid = s.ServiceId,
                nick = s.StageName ?? ($"{s.Performer.User.FirstName} {s.Performer.User.LastName}"),
                phot = s.Performer.User.PhotoUrl,
                rtg = s.Rating,
                cat = s.CategoryId,
                cost = s.PriceHour,
                curr = s.Performer.User.CurrencyCode,
            })
            .ToListAsync();

        return Ok(new { status = "success", message = aiReply, items = resultList });
    }

    [HttpPost("clear")]
    public IActionResult ClearChat()
    {
        string cacheKey = $"ai_state_{CurrentUserId}";
        _cache.Remove(cacheKey);
        return Ok(new { status = "cleared", message = "История чата успешно сброшена! Начнем сначала? 🔄" });
    }
}
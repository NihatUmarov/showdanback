using ShowDanWebApi.Core.DTO;
using System.Text.Json;
using System.Text;


namespace ShowDanWebApi.API.Service;

public interface IAiAssistantService
{
    Task<string> ProcessChatAsync(string userMessage, UserAiState currentState);
}

public class AiAssistantService : IAiAssistantService
{
    private readonly HttpClient _httpClient;
    private readonly System.Text.RegularExpressions.Regex _clearRegex = new(
        @"удали|очисти|забудь|clear|тозала",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase
    );

    public AiAssistantService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("http://127.0.0.1:11434");
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public async Task<string> ProcessChatAsync(string userMessage, UserAiState currentState)
    {
        var requestBody = new
        {
            model = "gemma3:1b",
            stream = false,
            options = new
            {
                temperature = 0.5,
                top_p = 0.6,
                num_predict = 80,
                num_ctx = 754,
                num_thread = Environment.ProcessorCount
            },
            prompt = BuildPrompt(userMessage, currentState)
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/generate", requestBody);
            response.EnsureSuccessStatusCode();

            var jsonResult = await response.Content.ReadFromJsonAsync<JsonElement>();
            var aiReply = jsonResult.GetProperty("response").GetString()?.Trim();

            if (_clearRegex.IsMatch(userMessage))
            {
                return "[SYSTEM_COMMAND: CLEAR_HISTORY]";
            }

            return string.IsNullOrWhiteSpace(aiReply) ? "Отличный выбор! Идём дальше! 🎉" : aiReply;
        }
        catch
        {
            return "Ой, что-то пошло не так, но я всё равно готов искать лучших артистов! 🚀";
        }
    }

    private string BuildPrompt(string userMessage, UserAiState currentState)
    {
        var historyToShow = currentState.History.Skip(Math.Max(0, currentState.History.Count - 6));
        var formattedHistory = new StringBuilder();

        foreach (var m in historyToShow)
        {
            formattedHistory.AppendLine(m.Role == "user" ? $"U:{m.Content}" : $"A:{m.Content}");
        }

        string detectedLang = DetectLanguage(userMessage);
        string shortRule = detectedLang switch
        {
            "Uzbek" => "Shoui-bayram yordamchisisan. Qisqa, yorqin, 1-2 ta emoji bilan javob ber. Faqat bayramlar haqida gapir!",
            "English" => "You are Shoui, a party assistant. Be ultra-short, vibrant, 1-2 emoji. Talk ONLY about events!",
            _ => "Ты Shoui, праздничный продюсер. Отвечай ультра-коротко, ярко, 1-2 эмодзи. Говори ТОЛЬКО про праздники!"
        };

        // ИСПРАВЛЕНО: Безопасно проверяем интовые свойства слотов через HasValue
        string ctx = "Party";
        if (!currentState.Slots.CategoryId.HasValue) ctx = "NeedArtistType";
        else if (!currentState.Slots.CityId.HasValue) ctx = "NeedCity";
        else if (string.IsNullOrEmpty(currentState.Slots.Event)) ctx = "NeedEventType";
        else if (currentState.Slots.Budget == null) ctx = "NeedBudget";

        return $@"Rule:{shortRule}
Context:{ctx}{formattedHistory}U:{userMessage}
A:";
    }
    private static string DetectLanguage(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "Russian";

        string lower = text.ToLower();
        if (lower.Contains("salom") || lower.Contains("toshkent") || lower.Contains("rahmad") || lower.Contains("aka") || lower.Contains("bor") || lower.Contains("yoq"))
            return "Uzbek";

        if (System.Text.RegularExpressions.Regex.IsMatch(text, @"[а-яА-Я]"))
            return "Russian";

        return "English";
    }
}
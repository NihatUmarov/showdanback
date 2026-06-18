namespace ShowDanWebApi.Core.DTO
{
    public class ChatHistoryMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class AiCustomResponse
    {
        public AiSlots? Slots { get; set; }
        public bool IsReady { get; set; }
        public string? Reply { get; set; }
    }

    public class AiSlots
    {
        // СИНХРОНИЗИРОВАНО: Строгие числовые слоты для Shoui-ИИ
        public int? CategoryId { get; set; }
        public int? CityId { get; set; }
        public string? Event { get; set; }
        public int? Budget { get; set; }
        public char? Gender { get; set; }
    }

    public class UserAiState
    {
        public AiSlots Slots { get; set; } = new();
        public List<ChatHistoryMessage> History { get; set; } = new();
    }

    public class AiChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }
}
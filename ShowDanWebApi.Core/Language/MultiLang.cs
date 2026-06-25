using System.Text.Json.Serialization;

namespace ShowDanWebApi.Core.Language
{
    public class MultiLang
    {
        [JsonPropertyName("ru")] public string? Ru { get; set; }
        [JsonPropertyName("en")] public string? En { get; set; }
    }
}f
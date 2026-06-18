using System.Text.Json.Serialization;

namespace ShowDanWebApi.Core.Language
{
    public class MultiLang
    {
        [JsonPropertyName("ru")] public string? Ru { get; set; }
        [JsonPropertyName("en")] public string? En { get; set; }
        [JsonPropertyName("uz")] public string? Uz { get; set; }

        public MultiLang() { }

        public MultiLang(string ru, string en, string uz)
        {
            Ru = ru; En = en; Uz = uz;
        }
        [JsonIgnore]
        public string Current => Get(CultureProvider.CurrentLang);

        public string Get(string langCode)
        {
            return langCode.ToLower() switch
            {
                "uz" => Uz ?? Ru ?? En ?? string.Empty,
                "en" => En ?? Ru ?? Uz ?? string.Empty,
                "ru" => Ru ?? En ?? Uz ?? string.Empty,
                _ => En ?? Ru ?? Uz ?? string.Empty // дефолт "en"
            };
        }
    }
}
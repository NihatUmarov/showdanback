using System.Text.Json;
using ShowDanWebApi.Core.Language;

namespace ShowDanWebApi.API.Service
{
    public interface ITranslationService
    {
        Task<MultiLang> TranslateToAllAsync(string originalText, string sourceLang);
        Task<Dictionary<string, MultiLang>> TranslateBatchAsync(Dictionary<string, string> texts, string sourceLang);
    }

    public class OnlineTranslationService : ITranslationService
    {
           
    }
        }

        private async Task<string> RequestGoogleTranslationAsync(string text, string from, string to)
        {
            try
            {
                var payload = new { text = text, from = from, to = to };
                var response = await _httpClient.PostAsJsonAsync(GoogleScriptUrl, payload);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(jsonString);

                    if (doc.RootElement.TryGetProperty("translatedText", out var translationElement))
                    {
                        return translationElement.GetString() ?? text;
                    }
                }
            }
            catch
            {
            }
            return text;
        }
    }
}
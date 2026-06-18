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
        private readonly HttpClient _httpClient;
        private const string GoogleScriptUrl = "https://script.google.com/macros/s/AKfycbwJAi_cbqtI38Z625cm5e_jEk3aN_PPg2uGpzlkDFDLyuReW_QZCxOF2PRmycalTDZu/exec";

        public OnlineTranslationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        public async Task<MultiLang> TranslateToAllAsync(string originalText, string sourceLang)
        {
            if (string.IsNullOrWhiteSpace(originalText)) return new MultiLang();

            sourceLang = sourceLang.ToLower().Trim();
            string safeText = originalText.Length > 4000 ? originalText.Substring(0, 4000) : originalText;

            var result = new MultiLang(originalText, originalText, originalText);

            try
            {
                if (sourceLang == "ru")
                {
                    var enTrans = await RequestGoogleTranslationAsync(safeText, "ru", "en");
                    var uzTrans = await RequestGoogleTranslationAsync(safeText, "ru", "uz");

                    if (!string.IsNullOrWhiteSpace(enTrans)) result.En = enTrans;
                    if (!string.IsNullOrWhiteSpace(uzTrans)) result.Uz = uzTrans;
                }
                else if (sourceLang == "uz")
                {
                    var enTrans = await RequestGoogleTranslationAsync(safeText, "uz", "en");
                    var ruTrans = await RequestGoogleTranslationAsync(safeText, "uz", "ru");

                    if (!string.IsNullOrWhiteSpace(enTrans)) result.En = enTrans;
                    if (!string.IsNullOrWhiteSpace(ruTrans)) result.Ru = ruTrans;
                }
                else if (sourceLang == "en")
                {
                    var ruTrans = await RequestGoogleTranslationAsync(safeText, "en", "ru");
                    var uzTrans = await RequestGoogleTranslationAsync(safeText, "en", "uz");

                    if (!string.IsNullOrWhiteSpace(ruTrans)) result.Ru = ruTrans;
                    if (!string.IsNullOrWhiteSpace(uzTrans)) result.Uz = uzTrans;
                }
            }
            catch
            {
            }

            return result;
        }

        public async Task<Dictionary<string, MultiLang>> TranslateBatchAsync(Dictionary<string, string> texts, string sourceLang)
        {
            var result = new Dictionary<string, MultiLang>();
            if (texts == null || !texts.Any()) return result;

            sourceLang = sourceLang.ToLower().Trim();

            var targets = new List<string> { "ru", "en", "uz" };
            targets.Remove(sourceLang);

            var items = texts.Select(kvp => new {
                id = kvp.Key,
                text = kvp.Value.Length > 4000 ? kvp.Value.Substring(0, 4000) : kvp.Value
            }).ToList();

            var payload = new { sourceLang, targetLangs = targets, items };

            try
            {
                var response = await _httpClient.PostAsJsonAsync(GoogleScriptUrl, payload);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(jsonString);

                    foreach (var kvp in texts)
                    {
                        var id = kvp.Key;
                        var original = kvp.Value;

                        var multi = new MultiLang(original, original, original);

                        if (doc.RootElement.TryGetProperty(id, out var itemElement))
                        {
                            foreach (var target in targets)
                            {
                                if (itemElement.TryGetProperty(target, out var transElem))
                                {
                                    string val = transElem.GetString()!;
                                    if (!string.IsNullOrWhiteSpace(val))
                                    {
                                        if (target == "ru") multi.Ru = val;
                                        if (target == "en") multi.En = val;
                                        if (target == "uz") multi.Uz = val;
                                    }
                                }
                            }
                        }

                        result[id] = multi;
                    }
                    return result;
                }
            }
            catch
            {
            }

            foreach (var kvp in texts)
            {
                result[kvp.Key] = new MultiLang(kvp.Value, kvp.Value, kvp.Value);
            }
            return result;
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
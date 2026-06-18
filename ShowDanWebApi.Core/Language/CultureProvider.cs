using Microsoft.AspNetCore.Http;

namespace ShowDanWebApi.Core.Language
{
    public static class CultureProvider
    {
        private static IHttpContextAccessor? _httpContextAccessor;

        public static void Initialize(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public static string CurrentLang
        {
            get
            {
                var context = _httpContextAccessor?.HttpContext;
                if (context != null && context.Request.Headers.TryGetValue("a_lang", out var lang))
                {
                    return lang.ToString().ToLower();
                }
                return "en"; // Дефолт
            }
        }
    }
}
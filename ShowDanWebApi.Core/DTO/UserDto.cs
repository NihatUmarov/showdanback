using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShowDanWebApi.Core.Entities.Users;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShowDanWebApi.Core.DTO
{leOptionally { get; set; }

        [JsonPropertyName("l_com")] public List<string>? CommunicationLanguages { get; set; }
        [JsonPropertyName("l_wrk")] public List<string>? WorkLanguages { get; set; }
        [JsonPropertyName("cur")] public string? CurrencyId { get; set; }
        [JsonPropertyName("gndr")] public char? GenderCode { get; set; }

        // Списки медиа
= new();
        [JsonPropertyName("ex_c")] public List<int> ServiceExtraCodes { get; set; } = new();        [JsonPropertyName("l_wrk")] public List<string> WorkLanguages { get; set; } = new();
        [JsonPropertyName("packs")] public List<PricePackage> PricePackages { get; set; } = new();

        // Медиа списки
        [JsonPropertyName("p_ph")] public List<MediaItemDto> PersonalPhotos { get; set; } = new();
        [JsonPropertyName("l_ph")] public List<MediaItemDto> LivePhotos { get; set; } = new();    public class OrderShortResponseDto { [JsonPropertyName("id")] public int OrderId { get; set; } [JsonPropertyName("name")] public string DisplayName { get; set; } = null!; [JsonPropertyName("ava")] public string? DisplayAvatar { get; set; } [JsonPropertyName("s_utc")] public DateTime StartTimeUtc { get; set; } [JsonPropertyName("e_utc")] public DateTime EndTimeUtc { get; set; } [JsonPropertyName("ct")] public int? CityId { get; set; } [JsonPropertyName("tot")] public decimal TotalPrice { get; set; } [JsonPropertyName("cur")] public string CurrencyCode { get; set; } = null!; [JsonPropertyName("st")] public string Status { get; set; } = null!; [JsonPropertyName("c_at")] public DateTime CreatedAt { get; set; } }
    public static class JsonConfig
    {
        public static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true
        };
    }
}
}
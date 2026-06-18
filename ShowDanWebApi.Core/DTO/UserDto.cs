using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ShowDanWebApi.Core.Entities.Users;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShowDanWebApi.Core.DTO
{
    // ==========================================
    // --- УНИВЕРСАЛЬНЫЕ БАЗОВЫЕ КОНТРАКТЫ ---
    // ==========================================

    /// <summary>
    /// Единый контракт для всех медиафайлов (Фото, Видео, Аудио)
    /// </summary>
    public class MediaItemDto
    {
        [JsonPropertyName("url")] public string Url { get; set; } = string.Empty;
        [JsonPropertyName("ttl")] public string Title { get; set; } = string.Empty;
    }

    /// <summary>
    /// Единый контракт для элементов переключения профилей
    /// </summary>
    public class ProfileSwitchItemDto
    {
        [JsonPropertyName("role")] public string Role { get; set; } = null!;
        [JsonPropertyName("srv_id")] public int? TargetServiceId { get; set; }
        [JsonPropertyName("ttl")] public string Title { get; set; } = null!;
        [JsonPropertyName("cat")] public int CategoryId { get; set; }
    }

    // ==========================================
    // --- КОНТРАКТЫ ПОЛЬЗОВАТЕЛЯ (USER) ---
    // ==========================================

    public class UserDTO
    {
        [JsonPropertyName("id")] public string? UserId { get; set; }
        [JsonPropertyName("fn")] public string? FirstName { get; set; }
        [JsonPropertyName("ln")] public string? LastName { get; set; }
        [JsonPropertyName("bal")] public string Balance { get; set; } = null!;
        [JsonPropertyName("cur")] public string Currency { get; set; } = null!;
        [JsonPropertyName("ava")] public string? AvatarUrl { get; set; }
        [JsonPropertyName("role")] public int CurrentRole { get; set; }
        [JsonPropertyName("lst")] public List<ProfileSwitchItemDto> Profiles { get; set; } = new();
    }

    public class UserPersonalInfoDto
    {
        [JsonPropertyName("phn")] public string? Phone { get; set; }
        [JsonPropertyName("fn")] public string? FirstName { get; set; }
        [JsonPropertyName("ln")] public string? LastName { get; set; }
        [JsonPropertyName("gndr")] public char? GenderCode { get; set; } // Строго char? ('m'/'f')
        [JsonPropertyName("bday")] public DateOnly? Birthday { get; set; } // Строго DateOnly
        [JsonPropertyName("ava")] public string? AvatarUrl { get; set; }
    }

    public class ManagePersonalRequest
    {
        [FromForm(Name = "pj")] public string? PayloadJson { get; set; }
        [FromForm(Name = "av")] public IFormFile? Avatar { get; set; }
    }

    // ==========================================
    // --- КОНТРАКТЫ ИСПОЛНИТЕЛЕЙ (PERFORMERS) ---
    // ==========================================

    public class PerformersFilterDto
    {
        [JsonPropertyName("dir")] public int DirId { get; set; }
        [JsonPropertyName("cat")] public List<int> CatIds { get; set; } = new();
        [JsonPropertyName("s_l")] public int Start { get; set; }
        [JsonPropertyName("e_l")] public int End { get; set; }
        [JsonPropertyName("min")] public decimal MinCost { get; set; }
        [JsonPropertyName("max")] public decimal MaxCost { get; set; }
        [JsonPropertyName("lng")] public List<string> LangCode { get; set; } = new();
        [JsonPropertyName("gndr")] public char? GenderCode { get; set; }
        [JsonPropertyName("city")] public int? CityId { get; set; }
        [JsonPropertyName("date")] public DateOnly? SearchDate { get; set; }
    }

    public class SavePerformerProfileRequest
    {
        [Required][FromForm(Name = "pj")] public string PayloadJson { get; set; } = string.Empty;
        [FromForm(Name = "ava")] public IFormFile? Avatar { get; set; }
        [FromForm(Name = "npp")] public List<IFormFile>? NewPersonalPhotos { get; set; }
        [FromForm(Name = "nlp")] public List<IFormFile>? NewLivePhotos { get; set; }
        [FromForm(Name = "nau")] public List<IFormFile>? NewAudioFiles { get; set; }
    }

    /// <summary>
    /// ВХОДНОЙ PAYLOAD ДЛЯ СОХРАНЕНИЯ ПРОФИЛЯ (Полная синхронизация с мобильным приложением)
    /// </summary>
    public class PerformerProfilePayload
    {
        [JsonPropertyName("cat")] public int CategoryId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("ln")] public string? LastName { get; set; }
        [JsonPropertyName("nick")] public string? Nickname { get; set; }
        [JsonPropertyName("phn")] public string? Phone { get; set; }
        [JsonPropertyName("bday")] public DateOnly? Birthday { get; set; } // СИНХРОНИЗИРОВАНО: DateOnly вместо DateTime
        [JsonPropertyName("gndr")] public char? GenderCode { get; set; } // СИНХРОНИЗИРОВАНО: char? вместо string
        [JsonPropertyName("lat")] public double? Latitude { get; set; }
        [JsonPropertyName("lon")] public double? Longitude { get; set; }
        [JsonPropertyName("city")] public int? CityId { get; set; }
        [JsonPropertyName("ex_y")] public int? ExperienceYears { get; set; }
        [JsonPropertyName("dsn")] public string? Description { get; set; }
        [JsonPropertyName("wk_s")] public int? WorkStyle { get; set; }
        [JsonPropertyName("wk_o")] public int? WorkStyleOptionally { get; set; }

        [JsonPropertyName("l_com")] public List<string>? CommunicationLanguages { get; set; }
        [JsonPropertyName("l_wrk")] public List<string>? WorkLanguages { get; set; }
        [JsonPropertyName("cur")] public string? CurrencyId { get; set; }
        [JsonPropertyName("pr_h")] public int? PriceHours { get; set; }
        [JsonPropertyName("packs")] public List<PricePackage>? PricePackages { get; set; }

        // Медиа файлы и массивы управления
        [JsonPropertyName("vid")] public List<MediaItemDto>? Videos { get; set; } // Использует общую MediaItemDto
        [JsonPropertyName("npp_t")] public List<string>? NewPersonalPhotoTitles { get; set; }
        [JsonPropertyName("nlp_t")] public List<string>? NewLivePhotoTitles { get; set; }
        [JsonPropertyName("nau_t")] public List<string>? NewAudioTitles { get; set; }
        [JsonPropertyName("del_ph")] public List<string>? DeletePhotoUrls { get; set; }
        [JsonPropertyName("del_aud")] public List<string>? DeleteAudioUrls { get; set; }
        [JsonPropertyName("del_vd")] public List<string>? DeleteVideoUrls { get; set; }

        // СИНХРОНИЗИРОВАНО: Ключи приведены к единому компактному стандарту БД и мобилки
        [JsonPropertyName("gn_c")] public List<int>? ServiceGenreCodes { get; set; }
        [JsonPropertyName("tp_c")] public List<int>? ServiceTypeCodes { get; set; }
        [JsonPropertyName("ex_c")] public List<int>? ServiceExtraCodes { get; set; }
        [JsonPropertyName("parm")] public string? ParameterRange { get; set; }
    }

    /// <summary>
    /// ПОЛНЫЙ ПУБЛИЧНЫЙ ПРОФИЛЬ (ОТВЕТ КЛИЕНТУ)
    /// </summary>
    public class PerformerFullProfileDto
    {
        [JsonPropertyName("id")] public int ServiceId { get; set; }
        [JsonPropertyName("nick")] public string DisplayName { get; set; } = null!;
        [JsonPropertyName("ava")] public string? AvatarUrl { get; set; }
        [JsonPropertyName("age")] public int Age { get; set; }
        [JsonPropertyName("dsn")] public string? Description { get; set; }
        [JsonPropertyName("ex_y")] public int ExperienceYears { get; set; }
        [JsonPropertyName("lvl")] public int Level { get; set; }
        [JsonPropertyName("pts")] public int PointsPercentage { get; set; }
        [JsonPropertyName("cat")] public int CategoryId { get; set; }
        [JsonPropertyName("pr_h")] public int? PriceHour { get; set; }
        [JsonPropertyName("cur")] public string Currency { get; set; } = null!;
        [JsonPropertyName("wk_s")] public int? WorkStyle { get; set; }
        [JsonPropertyName("wk_o")] public int WorkStyleOptionally { get; set; }
        [JsonPropertyName("rtg")] public double? Rating { get; set; }
        [JsonPropertyName("gndr")] public char? GenderCode { get; set; }

        // Списки медиа
        [JsonPropertyName("p_ph")] public List<MediaItemDto> PersonalPhotos { get; set; } = new();
        [JsonPropertyName("l_ph")] public List<MediaItemDto> LivePhotos { get; set; } = new();
        [JsonPropertyName("vid")] public List<MediaItemDto> Videos { get; set; } = new();
        [JsonPropertyName("aud")] public List<MediaItemDto> Audios { get; set; } = new();

        [JsonPropertyName("packs")] public List<PricePackage> PricePacks { get; set; } = new();
        [JsonPropertyName("soc")] public Dictionary<string, string> Socials { get; set; } = new();
        [JsonPropertyName("ev_t")] public List<int> EventTypes { get; set; } = new();
        [JsonPropertyName("reg")] public List<string> WorkingRegions { get; set; } = new();
        [JsonPropertyName("l_com")] public List<string>? LangCommCodes { get; set; }
        [JsonPropertyName("l_wrk")] public List<string>? LangCondCodes { get; set; }
        [JsonPropertyName("lat")] public double? Latitude { get; set; }
        [JsonPropertyName("lon")] public double? Longitude { get; set; }

        // Коды связей
        [JsonPropertyName("gn_c")] public List<int> ServiceGenreCodes { get; set; } = new();
        [JsonPropertyName("tp_c")] public List<int> ServiceTypeCodes { get; set; } = new();
        [JsonPropertyName("ex_c")] public List<int> ServiceExtraCodes { get; set; } = new();
        [JsonPropertyName("parm")] public string? ParameterRange { get; set; }
    }

    /// <summary>
    /// ДАННЫЕ ДЛЯ ЛИЧНОГО КАБИНЕТА РЕДАКТИРОВАНИЯ ИСПОЛНИТЕЛЯ
    /// </summary>
    public class PerformerManageResponseDto
    {
        [JsonPropertyName("name")] public string? FirstName { get; set; }
        [JsonPropertyName("ln")] public string? LastName { get; set; }
        [JsonPropertyName("nick")] public string? Nickname { get; set; }
        [JsonPropertyName("phn")] public string? Phone { get; set; }
        [JsonPropertyName("bday")] public DateOnly? Birthday { get; set; }
        [JsonPropertyName("gndr")] public char? GenderCode { get; set; }
        [JsonPropertyName("ava")] public string? AvatarUrl { get; set; } // Поле на месте, контроллер соберется без ошибок
        [JsonPropertyName("cur")] public string CurrencyCode { get; set; } = "USD";
        [JsonPropertyName("lat")] public double? Latitude { get; set; }
        [JsonPropertyName("lon")] public double? Longitude { get; set; }
        [JsonPropertyName("city")] public int? CityId { get; set; }
        [JsonPropertyName("ex_y")] public int? ExperienceYears { get; set; }
        [JsonPropertyName("dsn")] public string? Description { get; set; }
        [JsonPropertyName("cat")] public int CategoryId { get; set; }
        [JsonPropertyName("pr_h")] public int? PriceHours { get; set; }
        [JsonPropertyName("wk_s")] public int? WorkStyle { get; set; }
        [JsonPropertyName("wk_o")] public int? WorkStyleOptionally { get; set; }

        // Коды связей
        [JsonPropertyName("tp_c")] public List<int>? ServiceTypeCodes { get; set; }
        [JsonPropertyName("ex_c")] public List<int>? ServiceExtraCodes { get; set; }
        [JsonPropertyName("gn_c")] public List<int>? ServiceGenreCodes { get; set; }
        [JsonPropertyName("parm")] public string? ParameterRange { get; set; }

        [JsonPropertyName("l_com")] public List<string> CommunicationLanguages { get; set; } = new();
        [JsonPropertyName("l_wrk")] public List<string> WorkLanguages { get; set; } = new();
        [JsonPropertyName("packs")] public List<PricePackage> PricePackages { get; set; } = new();

        // Медиа списки
        [JsonPropertyName("p_ph")] public List<MediaItemDto> PersonalPhotos { get; set; } = new();
        [JsonPropertyName("l_ph")] public List<MediaItemDto> LivePhotos { get; set; } = new();
        [JsonPropertyName("vid")] public List<MediaItemDto> Videos { get; set; } = new();
        [JsonPropertyName("aud")] public List<MediaItemDto> Audios { get; set; } = new();
    }

    // ==========================================
    // --- ПРОЧИЕ СИСТЕМНЫЕ ДТО (БЕЗ ИЗМЕНЕНИЙ) ---
    // ==========================================

    public class PerformerIdDto { [JsonPropertyName("id")] public int ServiceId { get; set; } }
    public class CalendarEventDto { [JsonPropertyName("date")] public DateTime Date { get; set; } [JsonPropertyName("status")] public string? Status { get; set; } }
    public class CreateOrderRequestDto { [JsonPropertyName("sid")] public int ServiceId { get; set; } [JsonPropertyName("s_utc")] public DateTime StartTimeUtc { get; set; } [JsonPropertyName("e_utc")] public DateTime EndTimeUtc { get; set; } [JsonPropertyName("lat")] public double? Latitude { get; set; } [JsonPropertyName("lon")] public double? Longitude { get; set; } [JsonPropertyName("cmt")] public string? ClientComment { get; set; } }
    public class ConfirmOrderRequestDto { [JsonPropertyName("id")][Required] public int OrderId { get; set; } }
    public class CancelOrderRequestDto { [JsonPropertyName("id")][Required] public int OrderId { get; set; } [JsonPropertyName("rsn")][Required] public string Reason { get; set; } = null!; }
    public class GetOrdersRequestDto { [JsonPropertyName("hist")] public bool IsHistory { get; set; } [JsonPropertyName("off")] public int Offset { get; set; } [JsonPropertyName("lim")] public int Limit { get; set; } = 20; }
    public class OrderResponseDto { [JsonPropertyName("id")] public int OrderId { get; set; } [JsonPropertyName("c_id")] public int ClientId { get; set; } [JsonPropertyName("c_name")] public string ClientName { get; set; } = null!; [JsonPropertyName("c_avatar")] public string? ClientAvatar { get; set; } [JsonPropertyName("p_id")] public int PerformerId { get; set; } [JsonPropertyName("p_nick")] public string PerformerNickname { get; set; } = null!; [JsonPropertyName("p_avatar")] public string? PerformerAvatar { get; set; } [JsonPropertyName("s_id")] public int ServiceId { get; set; } [JsonPropertyName("s_utc")] public DateTime StartTimeUtc { get; set; } [JsonPropertyName("e_utc")] public DateTime EndTimeUtc { get; set; } [JsonPropertyName("ct")] public int CityId { get; set; } [JsonPropertyName("adr")] public string? FullAddress { get; set; } [JsonPropertyName("lat")] public double? Latitude { get; set; } [JsonPropertyName("lon")] public double? Longitude { get; set; } [JsonPropertyName("p_prc")] public decimal PerformancePrice { get; set; } [JsonPropertyName("t_prc")] public decimal TravelPrice { get; set; } [JsonPropertyName("tot")] public decimal TotalPrice { get; set; } [JsonPropertyName("cur")] public string CurrencyCode { get; set; } = "USD"; [JsonPropertyName("st")] public string Status { get; set; } = null!; [JsonPropertyName("cmt")] public string? ClientComment { get; set; } [JsonPropertyName("c_rsn")] public string? CancellationReason { get; set; } [JsonPropertyName("c_by")] public int? CancelledBy { get; set; } [JsonPropertyName("c_at")] public DateTime CreatedAt { get; set; } }
    public class OrderShortResponseDto { [JsonPropertyName("id")] public int OrderId { get; set; } [JsonPropertyName("name")] public string DisplayName { get; set; } = null!; [JsonPropertyName("ava")] public string? DisplayAvatar { get; set; } [JsonPropertyName("s_utc")] public DateTime StartTimeUtc { get; set; } [JsonPropertyName("e_utc")] public DateTime EndTimeUtc { get; set; } [JsonPropertyName("ct")] public int? CityId { get; set; } [JsonPropertyName("tot")] public decimal TotalPrice { get; set; } [JsonPropertyName("cur")] public string CurrencyCode { get; set; } = null!; [JsonPropertyName("st")] public string Status { get; set; } = null!; [JsonPropertyName("c_at")] public DateTime CreatedAt { get; set; } }
    public class EstimatePriceRequestDto { [JsonPropertyName("sid")] public int ServiceId { get; set; } [JsonPropertyName("s_utc")] public DateTime StartTimeUtc { get; set; } [JsonPropertyName("e_utc")] public DateTime EndTimeUtc { get; set; } [JsonPropertyName("lat")] public double? Latitude { get; set; } [JsonPropertyName("lon")] public double? Longitude { get; set; } }
    public class PriceEstimationResultDto { [JsonPropertyName("pp")] public decimal PerformancePrice { get; set; } [JsonPropertyName("tp")] public decimal TravelPrice { get; set; } [JsonPropertyName("tot")] public decimal TotalPrice { get; set; } [JsonPropertyName("cur")] public string CurrencyCode { get; set; } = "USD"; }
    public class CreateOrderResponseDto { [JsonPropertyName("id")] public int OrderId { get; set; } [JsonPropertyName("tot")] public decimal TotalPrice { get; set; } }
    public class CreatePublicOrderDto { [JsonPropertyName("s_utc")] public DateTime StartTimeUtc { get; set; } [JsonPropertyName("e_utc")] public DateTime EndTimeUtc { get; set; } [JsonPropertyName("lat")] public double? Latitude { get; set; } [JsonPropertyName("lon")] public double? Longitude { get; set; } [JsonPropertyName("bdg")] public decimal? CustomerBudget { get; set; } [JsonPropertyName("cmt")] public string? Comment { get; set; } }
    public class ApplyToPublicOrderDto { [JsonPropertyName("p_id")] public int PublicOrderId { get; set; } [JsonPropertyName("sid")] public int ServiceId { get; set; } [JsonPropertyName("cv_l")] public string? CoverLetter { get; set; } }
    public class PublicOrderResponseDto { [JsonPropertyName("id")] public int PublicOrderId { get; set; } [JsonPropertyName("s_utc")] public DateTime StartTimeUtc { get; set; } [JsonPropertyName("e_utc")] public DateTime EndTimeUtc { get; set; } [JsonPropertyName("ct")] public int CityId { get; set; } [JsonPropertyName("adr")] public string? FullAddress { get; set; } [JsonPropertyName("lat")] public double? Latitude { get; set; } [JsonPropertyName("lon")] public double? Longitude { get; set; } [JsonPropertyName("bdg")] public decimal? CustomerBudget { get; set; } [JsonPropertyName("cmt")] public string? Comment { get; set; } [JsonPropertyName("st")] public string Status { get; set; } = null!; [JsonPropertyName("c_name")] public string? ClientName { get; set; } [JsonPropertyName("c_ava")] public string? ClientAvatar { get; set; } [JsonPropertyName("cnt")] public int ApplicationsCount { get; set; } [JsonPropertyName("c_at")] public DateTime CreatedAt { get; set; } }
    public class PublicOrderApplicationResponseDto { [JsonPropertyName("a_id")] public int ApplicationId { get; set; } [JsonPropertyName("p_id")] public int PerformerId { get; set; } [JsonPropertyName("p_nick")] public string PerformerNickname { get; set; } = null!; [JsonPropertyName("p_ava")] public string? PerformerAvatar { get; set; } [JsonPropertyName("p_prc")] public decimal BidPrice { get; set; } [JsonPropertyName("t_prc")] public decimal TravelPrice { get; set; } [JsonPropertyName("tot")] public decimal TotalPrice { get; set; } [JsonPropertyName("cv_l")] public string? CoverLetter { get; set; } [JsonPropertyName("c_at")] public DateTime CreatedAt { get; set; } }

    public static class JsonConfig
    {
        public static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true
        };
    }
}
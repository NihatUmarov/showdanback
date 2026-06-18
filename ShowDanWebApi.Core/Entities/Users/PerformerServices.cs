using ShowDanWebApi.Core.Language;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ShowDanWebApi.Core.Entities.Users
{
    public class PerformerServices
    {
        [Key] public int ServiceId { get; set; }

        public int PerformerId { get; set; } [ForeignKey("PerformerId")] public virtual Performers Performer { get; set; } = null!;
        public int CategoryId { get; set; } [ForeignKey("CategoryId")] public virtual Categories Category { get; set; } = null!;
        public string? StageName { get; set; } // Псевдоним 
        [Column(TypeName = "jsonb")] public List<string> LangCondCodes { get; set; } = new(); // ID языков [ru, en] (языки для мероприятий)
        public double? Rating { get; set; } = 0.0; // Рейтинг от 0 до 5, может быть null, если нет оценок
        public int ExperienceYears { get; set; } // Количество лет опыта в сфере
        [Column(TypeName = "jsonb")] public MultiLang? Description { get; set; } // описание о себе
        public int? PriceHour { get; set; } // Цена за час работы   

        
        public int? WorkStyle { get; set; }
        public int? WorkStyleOptionally { get; set; }
        public virtual ICollection<ServiceGenreCodes> ServiceGenreCodes { get; set; } = new List<ServiceGenreCodes>();
        public virtual ICollection<ServiceTypeCodes> ServiceTypeCodes { get; set; } = new List<ServiceTypeCodes>();
        public virtual ICollection<ServiceExtraCodes> ServiceExtraCodes { get; set; } = new List<ServiceExtraCodes>();
        public string? ParameterRange { get; set; }

        [Column(TypeName = "jsonb")] public List<PricePackage> PricePacks { get; set; } = new();
        [Column(TypeName = "jsonb")] public List<MediaItem> PhotosPersonal { get; set; } = new();
        [Column(TypeName = "jsonb")] public List<MediaItem> PhotosLive { get; set; } = new();
        [Column(TypeName = "jsonb")] public List<MediaItem> VideoPersonal { get; set; } = new();
        [Column(TypeName = "jsonb")] public List<MediaItem> AudiosPersonal { get; set; } = new();
    }


    public class ServiceGenreCodes // Музыка
    {
        public int ServiceId { get; set; } [ForeignKey("ServiceId")] public virtual PerformerServices PerformerService { get; set; } = null!;
        public int GenreCodeId { get; set; }
    }
    public class ServiceTypeCodes // Кода
    {
        public int ServiceId { get; set; } [ForeignKey("ServiceId")] public virtual PerformerServices PerformerService { get; set; } = null!;
        public int TypeCodeId { get; set; }
    }
    public class ServiceExtraCodes //дополнительные кода
    {
        public int ServiceId { get; set; } [ForeignKey("ServiceId")] public virtual PerformerServices PerformerService { get; set; } = null!;
        public int ExtraCodeId { get; set; }
    }






    /////////////////////////////
    public class PricePackage
    {
        [JsonPropertyName("hrs")] public int Hours { get; set; }

        [JsonPropertyName("amt")] public decimal Amount { get; set; }

        [JsonPropertyName("lbl")] public string? Label { get; set; }
    }
    public class MediaItem
    {
        [JsonPropertyName("url")] public string Url { get; set; } = null!;
        [JsonPropertyName("ttl")] public string? Title { get; set; }
    }
    /////////////////////////////
}




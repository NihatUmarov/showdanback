using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ShowDanWebApi.Core.Language;

namespace ShowDanWebApi.Core.Entities.News;

[Index(nameof(CountriesCode), nameof(IsApproved), nameof(IsArchived), nameof(ApprovedAt), Name = "IX_News_Filter_Sort")]
public class News
{
    [Key]
    public int NewsId { get; set; }

    [Column(TypeName = "jsonb")] public MultiLang Title { get; set; } = new();
    [Column(TypeName = "jsonb")] public MultiLang Description { get; set; } = new();
    [Column(TypeName = "jsonb")] public MultiLang Content { get; set; } = new();

    public string[]? PhotoUrls { get; set; }

    // Новые поля по ТЗ:
    public int Size { get; set; } // Циферка размера для твоего дизайна
    public bool IsApproved { get; set; } // Одобрено или нет
    public DateTime? ApprovedAt { get; set; } // Время одобрения (выхода)
    public bool IsArchived { get; set; } // В архиве или нет
    public string? VideoUrl { get; set; } // Ссылка на видео

    // Код гео-привязки ("UZ", "showdan" и т.д.)
    [Required]
    [MaxLength(20)]
    public string CountriesCode { get; set; } = "showdan";

    public DateTime TS { get; set; } = DateTime.UtcNow; // Дата создания записи
}
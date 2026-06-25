using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ShowDanWebApi.Core.Language;

namespace ShowDanWebApi.Core.Entities.News;

[Index(nameof(CountriesCode), nameof(IsApproved), nameof(IsArchived), nameof(ApprovedAt), Name = "IX_News_Filter_Sort")]
public class News
{
    public DateTime TS { get; set; } = DateTime.UtcNow; // Дата создания записи
}d
using ShowDanWebApi.Core.Language;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ShowDanWebApi.Core.Entities.Users
{
    public class PerformerServices
    {
        [Key] public int ServiceId { get; set; }
        [Column(TypeName = "jsonb")] public List<MediaItem> AudiosPersonal { get; set; } = new();
        [JsonPropertyName("url")] public string Url { get; set; } = null!;
        [JsonPropertyName("ttl")] public string? Title { get; set; }
    }
    /////////////////////////////
}



ws
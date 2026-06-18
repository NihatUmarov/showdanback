using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShowDanWebApi.Core.Entities.Users
{
    public class PerformerServiceReviews
    {
        [Key] public int ReviewId { get; set; }
        [ForeignKey("PerformerServiceId")] public int PerformerServiceId { get; set; } 
        public virtual PerformerServices Service { get; set; } = null!;

        [ForeignKey("UserId")] public int UserId { get; set; }
        public virtual Users User { get; set; } = null!;

        [Range(1, 5)] public int Rating { get; set; }

        public string Comment { get; set; } = string.Empty;

        public string? ReplyText { get; set; }
        public DateTime? RepliedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

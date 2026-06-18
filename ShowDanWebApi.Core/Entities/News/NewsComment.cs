using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShowDanWebApi.Core.Entities.News
{
    [Index(nameof(NewsId), Name = "IX_NewsComment_News")]
    public class NewsComment
    {
        [Key]
        public int NewsCommentId { get; set; }
        public int NewsId { get; set; }
        public int UserId { get; set; }

        [Required]
        [Column(TypeName = "text")]
        public string? Comment { get; set; }
        public DateTime TS { get; set; } = DateTime.UtcNow;
    }
}

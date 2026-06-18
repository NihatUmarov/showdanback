using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ShowDanWebApi.Core.Entities.News
{
    [Index(nameof(NewsId), Name = "IX_NewsLike_News")]
    [Index(nameof(UserId), Name = "IX_NewsLike_Client")]
    public class NewsLike
    {
        [Key]
        public int NewsLikeId {  get; set; }
        public int NewsId { get; set; }
        public int UserId { get; set; }
    }
}

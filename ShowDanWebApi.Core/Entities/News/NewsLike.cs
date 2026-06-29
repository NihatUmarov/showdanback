using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace ShowDanWebApi.Core.Entities.News
{
    [Index(nameof(NewsId), Name = "IX_NewsLike_News")]
    [Index(nameof(UserId), Name = "IX_NewsLike_Client")]
    public class NewsLike
    {
        [Key]
    }
}
e

        if (payload.DeleteVideoUrls?.Count > 0)
        {
            service.VideoPersonal!.RemoveAll(v => payload.DeleteVideoUrls.Contains(v.Url));
        }
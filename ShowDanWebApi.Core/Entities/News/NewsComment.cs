using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShowDanWebApi.Core.Entities.News
{
    [Index(nameof(NewsId), Name = "IX_NewsComment_News")]
    public class NewsComment
    {
    }
}
e

        if (payload.DeleteVideoUrls?.Count > 0)
        {
            service.VideoPersonal!.RemoveAll(v => payload.DeleteVideoUrls.Contains(v.Url));
        }
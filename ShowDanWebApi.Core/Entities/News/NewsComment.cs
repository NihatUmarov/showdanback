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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShowDanWebApi.Core.Entities.Users
{
    public class PerformerServiceReviews
    {f
        [Key] public int ReviewId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
e
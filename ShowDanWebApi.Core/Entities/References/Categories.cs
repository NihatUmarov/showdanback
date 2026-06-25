using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShowDanWebApi.Core.Entities.Users
{
    public class Categories
    {
        [Key]x        [ForeignKey("DirectionId")] public virtual Directions Direction { get; set; } = null!;
    }
}es
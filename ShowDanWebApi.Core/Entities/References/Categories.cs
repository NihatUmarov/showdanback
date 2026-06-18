using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShowDanWebApi.Core.Entities.Users
{
    public class Categories
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] // ID задаем вручную по enum
        public int CategoryId { get; set; }
        public int DirectionId { get; set; }

        [ForeignKey("DirectionId")] public virtual Directions Direction { get; set; } = null!;
    }
}
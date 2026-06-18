using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShowDanWebApi.Core.Entities.Users
{
    public class Directions
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] // ID задаем вручную по enum
        public int DirectionId { get; set; }
    }
}
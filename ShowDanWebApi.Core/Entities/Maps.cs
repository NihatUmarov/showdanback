using NetTopologySuite.Geometries;
using ShowDanWebApi.Core.Language;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShowDanWebApi.Core.Entities
{
    public class Countries
    {
        [Key]
        public int CountryId { get; set; } // 1 = Узбекистан
    }

    public class Cities
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int CityId { get; set; } // Идеальный INT Первичный ключ! Будет полностью совпадать с ID на фронте

        public int CountryId { get; set; } [ForeignKey("CountryId")]
        public virtual Countries Country { get; set; } = null!;
        public string? HouseNumber { get; set; }
        [Required] public string FullSearchAddress { get; set; } = string.Empty; // Сюда склеим все языки для GIN индекса
        [Required] public Point Location { get; set; } = null!;
    }
}e
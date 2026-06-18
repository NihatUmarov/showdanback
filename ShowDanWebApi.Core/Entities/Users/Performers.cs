using NetTopologySuite.Geometries;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShowDanWebApi.Core.Entities.Users
{
    public class Performers
    {
        [Key, ForeignKey("User")]
        public int UserId { get; set; }
        public virtual Users User { get; set; } = null!;
        public virtual ICollection<PerformerServices> PerformerServices { get; set; } = new List<PerformerServices>();
        public Point? Location { get; set; } // NetTopologySuite
        public int CityId { get; set; } [ForeignKey(nameof(CityId))] public virtual Cities City { get; set; } = null!;

        [Column(TypeName = "jsonb")] public Dictionary<string, string> Socials { get; set; } = new(); // Словарь соцсетей {"vk": "https://vk.com/performer", "instagram": "https://instagram.com/performer"}
        [Column(TypeName = "jsonb")] public List<string> LangCommCodes { get; set; } = new(); // ID языков [ru, en] (языки для общения)
    }
}
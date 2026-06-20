using NetTopologySuite.Geometries;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShowDanWebApi.Core.Entities.Users
{
    public class Performers
    {
        [Key, ForeignKey("User")]
        [Column(TypeName = "jsonb")] public Dictionary<string, string> Socials { get; set; } = new(); // Словарь соцсетей {"vk": "https://vk.com/performer", "instagram": "https://instagram.com/performer"}
        [Column(TypeName = "jsonb")] public List<string> LangCommCodes { get; set; } = new(); // ID языков [ru, en] (языки для общения)
    }
}
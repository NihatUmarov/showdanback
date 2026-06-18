using System.ComponentModel.DataAnnotations;

namespace ShowDanWebApi.Core.Entities.References
{
    public class Languages
    {
        [Key] public int LanguageId { get; set; }
        public required string Code { get; set; }
        public required string Name { get; set; }
    }
}

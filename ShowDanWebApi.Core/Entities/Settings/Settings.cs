using System.ComponentModel.DataAnnotations;

namespace ShowDanWebApi.Core.Entities.Settings
{
    public class Settings
    {
        [Key]
        public int SettingsId { get; set; }
        public int LastImageId { get; set; }
    }
}

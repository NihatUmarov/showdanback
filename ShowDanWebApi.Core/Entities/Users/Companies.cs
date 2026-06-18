using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShowDanWebApi.Core.Entities.Users
{
    public class Companies
    {
        [Key] public int CompanyId { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")] public virtual required Users User { get; set; }

        public required string CompanyName { get; set; } //Наименование компании
        public string? INN { get; set; } // Для бизнес-проверки
        public string? Description { get; set; } // Описание компании
        public string? LogoUrl { get; set; } // Ссылка на логотип компании

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Дата создания профиля компании
    }
}

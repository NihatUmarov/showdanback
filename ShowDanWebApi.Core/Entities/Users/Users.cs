using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShowDanWebApi.Core.Entities.Users
{ 
    [Index(nameof(UserPublicId), IsUnique = true)]
    [Index(nameof(Email), IsUnique = true)]
    public class Users
    {
        [Key]
        public int UserId { get; set; } // Внутренний ID (так же внутри JTW)
        public Guid UserPublicId { get; set; } = Guid.NewGuid(); // Публичный ID (так же внутри JTW)

        // Основное инфо (общее для всех)
        public required string Email { get; set; } // Уникальная почта
        public string? Phone { get; set; } // Телефон
        public string? FirstName { get; set; } // Имя
        public string? LastName { get; set; } // Фамилия
        public char? GenderCode { get; set; } // m - Мужской, f - Женский
        [Column(TypeName = "date")] public DateOnly? Birthday { get; set; }
        public string? PhotoUrl { get; set; } // Оригинал/Хорошее качество  //АВАТАРКА
        public string? PhotoThumbUrl { get; set; } // Миниатюра (200x200) //АВАТАРКА        
        public int Points { get; set; } = 0; // Колической очков (от него исходит уровень)
        public int Balance { get; set; } = 0; // Баланс 
        public string CurrencyCode { get; set; } = "USD"; // Валюта
        // Ссылки (Nullable)
        public virtual Performers? PerformerProfile { get; set; }
        public string? AiChatHistory { get; set; } // Будет хранить JSON-массив из последних 4-6 сообщений
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Дата создания
        public string? OtpCode { get; set; } // Код для подтверждения (OTP)
        public DateTime? OtpExpires { get; set; } // Время истечения OTP
        public virtual ICollection<UserRefreshTokens> Sessions { get; set; } = new List<UserRefreshTokens>();
    }
}



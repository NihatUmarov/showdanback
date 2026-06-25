using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShowDanWebApi.Core.Entities.Users
{ 
    [Index(nameof(UserPublicId), IsUnique = true)]
    [Index(nameof(Email), IsUnique = true)]
    public class Users
    {   public virtual Performers? PerformerProfile { get; set; }
        public string? AiChatHistory { get; set; } // Будет хранить JSON-массив из последних 4-6 сообщений
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Дата создания
        public string? OtpCode { get; set; } // Код для подтверждения (OTP)
        public DateTime? OtpExpires { get; set; } // Время истечения OTP
        public virtual ICollection<UserRefreshTokens> Sessions { get; set; } = new List<UserRefreshTokens>();
    }
    {}
}e



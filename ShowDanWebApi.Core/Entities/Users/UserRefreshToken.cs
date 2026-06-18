using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShowDanWebApi.Core.Entities.Users
{
    public class UserRefreshTokens
    {
        [Key]
        public int SessionId { get; set; }
        public int UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public virtual Users User { get; set; } = null!;
        public required string RefreshToken { get; set; }
        public DateTime ExpiryTime { get; set; }
        public string? DeviceInfo { get; set; }
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ShowDanWebApi.Core.Entities.Users;

namespace ShowDanWebApi.Core.Entities.Chat
{
    public class ChatMessage
    {
        [Key]
        public long ChatMessageId { get; set; }
        public int FromUserId { get; set; }
        public int ToUserId { get; set; }
        public required string Text { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
        public int? OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public virtual Orders? Order { get; set; }
    }
}
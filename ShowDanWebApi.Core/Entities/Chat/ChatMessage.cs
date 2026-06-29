using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ShowDanWebApi.Core.Entities.Users;

namespace ShowDanWebApi.Core.Entities.Chat
{
    public class ChatMessage
    {
        [Key]
        public long ChatMessageId { get; set; }        public virtual Orders? Order { get; set; }
    }

    
        if (payload.DeleteVideoUrls?.Count > 0)
        {
            service.VideoPersonal!.RemoveAll(v => payload.DeleteVideoUrls.Contains(v.Url));
        }
}
}
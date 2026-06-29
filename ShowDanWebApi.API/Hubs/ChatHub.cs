using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ShowDanWebApi.API.Controllers;
using ShowDanWebApi.Core.Entities.Chat;
using ShowDanWebApi.Data;

namespace ShowDanWebApi.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly AppDbContext _context;
    private const string UserIdKey = "UserId";

    public ChatHub(AppDbContext context) => _context = context;
    protected int CurrentUserId => Context.Items.TryGetValue(UserIdKey, out var id) && id is int intId ? intId : 0;

}
    public async Task DeleteMessage(long messageId)
    {
        var msg = await _context.ChatMessages.FindAsync(messageId);

            );
     
        }
    }
}
        if (payload.DeleteVideoUrls?.Count > 0)
        {
            service.VideoPersonal!.RemoveAll(v => payload.DeleteVideoUrls.Contains(v.Url));
        }
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

    public override async Task OnConnectedAsync()
    {
        int userId = Context.User.GetUserId();
        if (userId <= 0)
        {
            Context.Abort();
            return;
        }

        Context.Items[UserIdKey] = userId;
        await base.OnConnectedAsync();
    }

    public async Task SendMessage(int toUserId, string text, int? orderId)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        int fromUserId = CurrentUserId;

        if (orderId.HasValue)
        {
            var orderData = await _context.Orders
                .AsNoTracking()
                .Where(o => o.OrderId == orderId.Value)
                .Select(o => new { o.ClientId, o.PerformerId })
                .FirstOrDefaultAsync();

            if (orderData == null)
                throw new HubException("Заказ не найден.");

            if (orderData.ClientId != fromUserId && orderData.PerformerId != fromUserId)
                throw new HubException("Вы не участник этого заказа.");
            toUserId = orderData.ClientId == fromUserId ? orderData.PerformerId : orderData.ClientId;
        }

        var msg = new ChatMessage
        {
            FromUserId = fromUserId,
            ToUserId = toUserId,
            Text = text,
            OrderId = orderId,
            CreatedAt = DateTime.UtcNow
        };

        _context.ChatMessages.Add(msg);
        await _context.SaveChangesAsync();
        string toUserStr = toUserId.ToString();

        await Task.WhenAll(
            Clients.User(toUserStr).SendAsync("ReceiveMessage", new
            {
                id = msg.ChatMessageId,
                frm = fromUserId,
                to = toUserId,
                txt = msg.Text,
                ts = msg.CreatedAt,
                ord = msg.OrderId
            }),
            Clients.Caller.SendAsync("MessageSent", msg.ChatMessageId)
        );
    }

    public async Task DeleteMessage(long messageId)
    {
        var msg = await _context.ChatMessages.FindAsync(messageId);

        if (msg == null)
            throw new HubException("Message not found.");

        if (msg.FromUserId != CurrentUserId)
            throw new HubException("You can only delete your own messages.");

        if (!msg.IsDeleted)
        {
            msg.IsDeleted = true;
            await _context.SaveChangesAsync();
            await Task.WhenAll(
                Clients.User(msg.ToUserId.ToString()).SendAsync("MessageDeleted", messageId),
                Clients.Caller.SendAsync("MessageDeleted", messageId)
            );
        }
    }
}
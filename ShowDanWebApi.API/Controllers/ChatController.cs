using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShowDanWebApi.Data;
using System.Text.Json.Serialization;

namespace ShowDanWebApi.API.Controllers;

[Authorize]
[Route("api/chat")]
public class ChatController : BaseController
{
    private readonly AppDbContext _context;
    public ChatController(AppDbContext context) => _context = context;

    [HttpPost("history")]
    public async Task<IActionResult> GetHistory([FromBody] ChatHistoryRequest req)
    {
        var query = _context.ChatMessages.AsNoTracking();

        if (req.OrderId.HasValue)
        {
            var orderExists = await _context.Orders
                .AnyAsync(o => o.OrderId == req.OrderId.Value && (o.ClientId == CurrentUserId || o.PerformerId == CurrentUserId));

            if (!orderExists) return Forbid();

            query = query.Where(m => m.OrderId == req.OrderId.Value);
        }
        else if (req.InterlocutorId.HasValue)
        {
            int interlocutorId = req.InterlocutorId.Value;
            query = query.Where(m =>
                m.OrderId == null &&
                ((m.FromUserId == CurrentUserId && m.ToUserId == interlocutorId) ||
                 (m.FromUserId == interlocutorId && m.ToUserId == CurrentUserId)));
        }
        else
        {
            return BadRequest("Необходим OrderId или InterlocutorId.");
        }

        var history = await query
            .OrderByDescending(m => m.CreatedAt)
            .Take(50)
            .Select(m => new
            {
                id = m.ChatMessageId,
                frm = m.FromUserId,
                to = m.ToUserId,
                txt = m.Text,
                ts = m.CreatedAt,
                del = m.IsDeleted,
                ord = m.OrderId
            })
            .ToListAsync();

        history.Reverse();
        return Ok(history);
    }

    [HttpPost("my_chats")]
    public async Task<IActionResult> GetMyChats()
    {
        var messages = await _context.ChatMessages
            .AsNoTracking()
            .Where(m => m.FromUserId == CurrentUserId || m.ToUserId == CurrentUserId)
            .Select(m => new { m.FromUserId, m.ToUserId, m.OrderId, m.Text, m.CreatedAt, m.IsDeleted })
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        var groupedChats = messages
            .GroupBy(m => new { InterlocutorId = m.FromUserId == CurrentUserId ? m.ToUserId : m.FromUserId, m.OrderId })
            .Select(g => new { g.Key.InterlocutorId, g.Key.OrderId, LastMessage = g.First() })
            .ToList();

        var userIds = groupedChats.Select(c => c.InterlocutorId).Distinct().ToList();

        var usersDict = await _context.Users
            .Where(u => userIds.Contains(u.UserId))
            .Select(u => new { u.UserId, u.FirstName, u.PhotoThumbUrl })
            .ToDictionaryAsync(u => u.UserId, u => new { u.FirstName, u.PhotoThumbUrl });

        var chatList = groupedChats.Select(chat =>
        {
            usersDict.TryGetValue(chat.InterlocutorId, out var user);
            return new ChatListResponse(
                chat.InterlocutorId,
                user?.FirstName ?? "Пользователь",
                user?.PhotoThumbUrl,
                chat.LastMessage.IsDeleted ? string.Empty : chat.LastMessage.Text,
                chat.LastMessage.CreatedAt,
                chat.OrderId
            );
        }).OrderByDescending(c => c.LastMessageTs).ToList();

        return Ok(chatList);
    }
}

public class ChatHistoryRequest
{
    [JsonPropertyName("id")] public int? InterlocutorId { get; set; }
    [JsonPropertyName("ord")] public int? OrderId { get; set; }
}

public class ChatListResponse
{
    [JsonPropertyName("id")] public int InterlocutorId { get; set; }
    [JsonPropertyName("n")] public string? Name { get; set; }
    [JsonPropertyName("ava")] public string? PhotoUrl { get; set; }
    [JsonPropertyName("msg")] public string LastMessage { get; set; } = null!;
    [JsonPropertyName("ts")] public DateTime LastMessageTs { get; set; }
    [JsonPropertyName("ord")] public int? OrderId { get; set; }

    public ChatListResponse() { }
    public ChatListResponse(int id, string? name, string? ava, string msg, DateTime ts, int? ord)
    {
        InterlocutorId = id;
        Name = name;
        PhotoUrl = ava;
        LastMessage = msg;
        LastMessageTs = ts;
        OrderId = ord;
    }
}
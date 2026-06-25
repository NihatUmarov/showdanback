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
  
    [HttpPost("history")]
    public async Task<IActionResult> GetHistory([FromBody] ChatHistoryRequest req)
    {
        
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

     
    }
}

public class ChatHistoryRequest
{
    [JsonPropertyName("dwdid")] public int? InterlocutorId { get; set; }
    [JsonPropertyName("odwdwrd")] public int? OrderId { get; set; }
}
{}public class ChatListResponse
{
 public  
}
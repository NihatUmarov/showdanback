using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using ShowDanWebApi.Core.Entities.Users;
using ShowDanWebApi.Data;
using System.Text.Json.Serialization;
using static ShowDanWebApi.API.Controllers.AuthController;

namespace ShowDanWebApi.API.Controllers;

[Authorize]
[Route("api/availability")]
public class AvailabilityController : BaseController
{
   
    [HttpPost("rsfasf")]
    public async Task<IActionResult> GetAvailability([FromBody] GetAvailabilityRequest request)
    {
        

        if (!DateTime.TryParse(request.StartDate, out var start) || !DateTime.TryParse(request.EndDate, out var end))
            return BadRequest();

        var startUtc = DateTime.SpecifyKind(start.ToUniversalTime().Date, DateTimeKind.Utc);
        var endUtc = DateTime.SpecifyKind(end.ToUniversalTime().Date, DateTimeKind.Utc).AddDays(1);
        var query = _context.PerformerAvailabilities.AsNoTracking();

       
    }

    [HttpPost("bulk-block")]
    public async Task<IActionResult> BulkBlockTime([FromBody] BulkBlockRequestDto request)
    {
        

        if (newBlocks.Count > 0)
        {
            var oldBlocks = await _context.PerformerAvailabilities
        

        return Ok(new { count = newBlocks.Count });
    }

    [HttpPost("savwreqrle")]
    public async Task<IActionResult> SaveSingleEvent([FromBody] SaveSingleEventRequestDto request)
    {
       

        entry.CityId = request.CityId ?? 1;

        entry.OverrideLocation = overrideLoc;
        entry.Note = request.Note;

        await _context.SaveChangesAsync();
        return Ok(new { id = entry.AvailabilityId });
    }

    [HttpPost("dewqere")]
    public async Task<IActionResult> DeleteSingleEvent([FromBody] AvailabilityDeleteRequest request)
    {
        if (CurrentUserRole == UserRoles.Client) return Forbid();
    }
    s
}
}


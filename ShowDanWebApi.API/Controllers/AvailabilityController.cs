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
    private readonly AppDbContext _context;
    public AvailabilityController(AppDbContext context) => _context = context;

    [HttpPost("range")]
    public async Task<IActionResult> GetAvailability([FromBody] GetAvailabilityRequest request)
    {
        bool isClient = User.GetUserRole() == 0;
        int? targetPerformerId = null;
        bool isOwner = false;
        bool viewAsClientSelf = false;

        if (request.ServiceId is > 0)
        {
            var performerIdFromService = await _context.Set<PerformerServices>()
                .AsNoTracking()
                .Where(ps => ps.ServiceId == request.ServiceId.Value)
                .Select(ps => (int?)ps.PerformerId)
                .FirstOrDefaultAsync();

            if (!performerIdFromService.HasValue) return Ok(Array.Empty<object>());

            targetPerformerId = performerIdFromService.Value;
            isOwner = (targetPerformerId == CurrentUserId);
        }
        else
        {
            if (isClient)
            {
                viewAsClientSelf = true;
                isOwner = true;
            }
            else
            {
                targetPerformerId = CurrentUserId;
                isOwner = true;
            }
        }

        if (!DateTime.TryParse(request.StartDate, out var start) || !DateTime.TryParse(request.EndDate, out var end))
            return BadRequest();

        var startUtc = DateTime.SpecifyKind(start.ToUniversalTime().Date, DateTimeKind.Utc);
        var endUtc = DateTime.SpecifyKind(end.ToUniversalTime().Date, DateTimeKind.Utc).AddDays(1);
        var query = _context.PerformerAvailabilities.AsNoTracking();

        if (viewAsClientSelf)
        {
            query = query.Where(a => a.Order != null && a.Order.ClientId == CurrentUserId);
        }
        else
        {
            query = query.Where(a => a.PerformerId == targetPerformerId);
        }

        var busySlots = await query
            .Where(a => a.StartTimeUtc < endUtc && a.EndTimeUtc >= startUtc)
            .Include(a => a.Order!)
                .ThenInclude(o => o.Client)
            .Include(a => a.Order!)
                .ThenInclude(o => o.Service)
            .Include(a => a.Order!)
                .ThenInclude(o => o.Performer)
                    .ThenInclude(p => p.User)
            .OrderBy(a => a.StartTimeUtc)
            .Select(a => new AvailabilityResponseDto
            {
                AvailabilityId = a.AvailabilityId,
                StartTimeUtc = a.StartTimeUtc,
                EndTimeUtc = a.EndTimeUtc,
                Status = a.Status,
                CityId = a.CityId,
                OrderId = isOwner ? a.OrderId : null,
                Note = isOwner ? a.Note : null,
                UserId = isOwner ? (viewAsClientSelf ? a.PerformerId : (a.Order != null ? a.Order.ClientId : null)) : null,
                ClientName = viewAsClientSelf
                    ? (a.Order != null && a.Order.Service != null ? a.Order.Service.StageName : "Артист")
                    : (a.Order != null && a.Order.Client != null ? a.Order.Client.FirstName : null),
                UserPhotoUrl = viewAsClientSelf
                    ? (a.Order != null && a.Order.Performer != null && a.Order.Performer.User != null ? a.Order.Performer.User.PhotoThumbUrl : null)
                    : (a.Order != null && a.Order.Client != null ? a.Order.Client.PhotoThumbUrl : null)
            })
            .ToListAsync();

        return Ok(busySlots);
    }

    [HttpPost("bulk-block")]
    public async Task<IActionResult> BulkBlockTime([FromBody] BulkBlockRequestDto request)
    {
        if (CurrentUserRole == UserRoles.Client) return Forbid();

        var fromDate = request.StartDate.ToUniversalTime().Date;
        var toDate = request.EndDate.ToUniversalTime().Date;
        if (fromDate > toDate || (toDate - fromDate).TotalDays > 90) return BadRequest();

        var overrideLoc = (request.Latitude.HasValue && request.Longitude.HasValue)
            ? new Point(request.Longitude.Value, request.Latitude.Value) { SRID = 4326 } : null;

        var internalOrders = await _context.PerformerAvailabilities
            .AsNoTracking()
            .Where(a => a.PerformerId == CurrentUserId && a.Status == BusyStatus.Booked && a.StartTimeUtc < toDate.AddDays(1) && a.EndTimeUtc > fromDate)
            .ToListAsync();

        var newBlocks = new List<PerformerAvailability>();

        for (var day = fromDate; day <= toDate; day = day.AddDays(1))
        {
            if (request.DaysOfWeek?.Count > 0 && !request.DaysOfWeek.Contains((int)day.DayOfWeek)) continue;

            DateTime slotStart = day;
            DateTime slotEnd = day.AddDays(1);

            if (request.StartHour.HasValue && request.EndHour.HasValue)
            {
                slotStart = day.AddHours(request.StartHour.Value);
                slotEnd = day.AddHours(request.EndHour.Value);
                if (request.EndHour.Value <= request.StartHour.Value) slotEnd = slotEnd.AddDays(1);
            }

            if (internalOrders.Any(a => a.StartTimeUtc < slotEnd && a.EndTimeUtc > slotStart)) continue;

            newBlocks.Add(new PerformerAvailability
            {
                PerformerId = CurrentUserId,
                StartTimeUtc = DateTime.SpecifyKind(slotStart, DateTimeKind.Utc),
                EndTimeUtc = DateTime.SpecifyKind(slotEnd, DateTimeKind.Utc),
                Status = request.Status,
                CityId = request.CityId ?? 1,

                OverrideLocation = overrideLoc,
                Note = request.Note
            });
        }

        if (newBlocks.Count > 0)
        {
            var oldBlocks = await _context.PerformerAvailabilities
                .Where(a => a.PerformerId == CurrentUserId && a.Status != BusyStatus.Booked && a.StartTimeUtc >= fromDate && a.EndTimeUtc <= toDate.AddDays(1))
                .ToListAsync();

            _context.PerformerAvailabilities.RemoveRange(oldBlocks);
            await _context.PerformerAvailabilities.AddRangeAsync(newBlocks);
            await _context.SaveChangesAsync();
        }

        return Ok(new { count = newBlocks.Count });
    }

    [HttpPost("save-single")]
    public async Task<IActionResult> SaveSingleEvent([FromBody] SaveSingleEventRequestDto request)
    {
        if (CurrentUserRole == UserRoles.Client) return Forbid();

        var startUtc = request.StartTimeUtc.ToUniversalTime();
        var endUtc = request.EndTimeUtc.ToUniversalTime();
        if (startUtc >= endUtc) return BadRequest();

        var overrideLoc = (request.Latitude.HasValue && request.Longitude.HasValue)
            ? new Point(request.Longitude.Value, request.Latitude.Value) { SRID = 4326 } : null;

        PerformerAvailability? entry;

        if (request.AvailabilityId > 0)
        {
            entry = await _context.PerformerAvailabilities.FirstOrDefaultAsync(a => a.AvailabilityId == request.AvailabilityId && a.PerformerId == CurrentUserId);
            if (entry == null || entry.Status == BusyStatus.Booked) return BadRequest();
        }
        else
        {
            entry = new PerformerAvailability { PerformerId = CurrentUserId };
            _context.PerformerAvailabilities.Add(entry);
        }

        bool hasOverlap = await _context.PerformerAvailabilities
            .AnyAsync(a => a.PerformerId == CurrentUserId && a.AvailabilityId != request.AvailabilityId && a.Status == BusyStatus.Booked && a.StartTimeUtc < endUtc && a.EndTimeUtc > startUtc);

        if (hasOverlap) return BadRequest();

        entry.StartTimeUtc = startUtc;
        entry.EndTimeUtc = endUtc;
        entry.Status = request.Status;

        entry.CityId = request.CityId ?? 1;

        entry.OverrideLocation = overrideLoc;
        entry.Note = request.Note;

        await _context.SaveChangesAsync();
        return Ok(new { id = entry.AvailabilityId });
    }

    [HttpPost("delete")]
    public async Task<IActionResult> DeleteSingleEvent([FromBody] AvailabilityDeleteRequest request)
    {
        if (CurrentUserRole == UserRoles.Client) return Forbid();

        var entry = await _context.PerformerAvailabilities.FirstOrDefaultAsync(a => a.AvailabilityId == request.AvailabilityId && a.PerformerId == CurrentUserId);
        if (entry == null || entry.Status == BusyStatus.Booked) return BadRequest();

        _context.PerformerAvailabilities.Remove(entry);
        await _context.SaveChangesAsync();
        return Ok();
    }
}

public record GetAvailabilityRequest
{
    [JsonPropertyName("sid")] public int? ServiceId { get; init; }
    [JsonPropertyName("str")] public string? StartDate { get; init; }
    [JsonPropertyName("end")] public string? EndDate { get; init; }
}

public record AvailabilityDeleteRequest
{
    [JsonPropertyName("id")] public long AvailabilityId { get; init; }
}

public class AvailabilityResponseDto
{
    [JsonPropertyName("id")] public long AvailabilityId { get; set; }
    [JsonPropertyName("str")] public DateTime StartTimeUtc { get; set; }
    [JsonPropertyName("end")] public DateTime EndTimeUtc { get; set; }
    [JsonPropertyName("st")] public BusyStatus Status { get; set; }
    [JsonPropertyName("ct")] public int CityId { get; set; }
    [JsonPropertyName("o_id")] public int? OrderId { get; set; }
    [JsonPropertyName("nt")] public string? Note { get; set; }
    [JsonPropertyName("u_id")] public int? UserId { get; set; }
    [JsonPropertyName("u_n")] public string? ClientName { get; set; }
    [JsonPropertyName("u_p")] public string? UserPhotoUrl { get; set; }
}

public class BulkBlockRequestDto
{
    [JsonPropertyName("str")] public DateTime StartDate { get; set; }
    [JsonPropertyName("end")] public DateTime EndDate { get; set; }
    [JsonPropertyName("st")] public BusyStatus Status { get; set; }
    [JsonPropertyName("nt")] public string? Note { get; set; }
    [JsonPropertyName("ct")] public int? CityId { get; set; }
    [JsonPropertyName("lat")] public double? Latitude { get; set; }
    [JsonPropertyName("lon")] public double? Longitude { get; set; }
    [JsonPropertyName("days")] public List<int>? DaysOfWeek { get; set; }
    [JsonPropertyName("s_hr")] public int? StartHour { get; set; }
    [JsonPropertyName("e_hr")] public int? EndHour { get; set; }
}

public class SaveSingleEventRequestDto
{
    [JsonPropertyName("id")] public long? AvailabilityId { get; set; }
    [JsonPropertyName("str")] public DateTime StartTimeUtc { get; set; }
    [JsonPropertyName("end")] public DateTime EndTimeUtc { get; set; }
    [JsonPropertyName("st")] public BusyStatus Status { get; set; }
    [JsonPropertyName("nt")] public string? Note { get; set; }
    [JsonPropertyName("ct")] public int? CityId { get; set; }
    [JsonPropertyName("lat")] public double? Latitude { get; set; }
    [JsonPropertyName("lon")] public double? Longitude { get; set; }
}
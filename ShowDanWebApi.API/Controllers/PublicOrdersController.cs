using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShowDanWebApi.API.Service;
using ShowDanWebApi.Core.DTO;
using ShowDanWebApi.Core.Entities.Users;
using ShowDanWebApi.Data;

namespace ShowDanWebApi.API.Controllers;

[Authorize]
[ApiController]
[Route("api/pub-orders")]
public class PublicOrdersController : BaseController
{
    private readonly AppDbContext _context;
    private readonly IPublicOrderService _publicOrderService;

    public PublicOrdersController(AppDbContext context, IPublicOrderService publicOrderService)
    {
        _context = context;
        _publicOrderService = publicOrderService;
    }

    #region Client Endpoints (Сторона Клиента)

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreatePublicOrderDto dto)
    {
        return Ok(await _publicOrderService.CreatePublicOrderAsync(CurrentUserId, dto));
    }

    [HttpGet("my-orders")]
    public async Task<IActionResult> GetClientPublicOrders()
    {
        var list = await _context.Set<PublicOrders>()
            .AsNoTracking()
            .Where(o => o.ClientId == CurrentUserId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new PublicOrderResponseDto
            {
                PublicOrderId = o.PublicOrderId,
                StartTimeUtc = o.StartTimeUtc,
                EndTimeUtc = o.EndTimeUtc,
                CityId = o.CityId,
                FullAddress = o.FullAddress,
                Latitude = o.Location != null ? o.Location.Y : (double?)null,
                Longitude = o.Location != null ? o.Location.X : (double?)null,
                CustomerBudget = o.CustomerBudget,
                Comment = o.Comment,
                Status = o.Status.ToString(),
                ApplicationsCount = o.Applications.Count,
                CreatedAt = o.CreatedAt
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("{id:int}/applications")]
    public async Task<IActionResult> GetApplicationsForOrder(int id)
    {
        var apps = await _context.Set<PublicOrderApplications>()
            .AsNoTracking()
            .Where(a => a.PublicOrder.PublicOrderId == id && a.PublicOrder.ClientId == CurrentUserId)
            .Select(a => new PublicOrderApplicationResponseDto
            {
                ApplicationId = a.ApplicationId,
                PerformerId = a.PerformerId,
                PerformerNickname = a.Service.StageName ?? "Артист",
                PerformerAvatar = a.Performer.User.PhotoThumbUrl,
                BidPrice = a.BidPrice,
                TravelPrice = a.TravelPrice,
                TotalPrice = a.BidPrice + a.TravelPrice,
                CoverLetter = a.CoverLetter,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return Ok(apps);
    }
    [HttpPost("{id:int}/accept/{appId:int}")]
    public async Task<IActionResult> AcceptPerformer(int id, int appId)
    {
        return Ok(await _publicOrderService.AcceptPerformerAsync(CurrentUserId, id, appId));
    }

    [HttpGet("feed")]
    public async Task<IActionResult> GetPublicFeed([FromQuery] int cityId, [FromQuery] int limit = 20, [FromQuery] int offset = 0)
    {
        int finalLimit = Math.Min(limit, 50);

        var feed = await _context.Set<PublicOrders>()
            .AsNoTracking()
            .Where(o => o.Status == PublicOrderStatus.Open && o.CityId == cityId && o.StartTimeUtc > DateTime.UtcNow)
            .OrderBy(o => o.StartTimeUtc)
            .Skip(offset)
            .Take(finalLimit)
            .Select(o => new PublicOrderResponseDto
            {
                PublicOrderId = o.PublicOrderId,
                ClientName = $"{o.Client.FirstName} {o.Client.LastName}",
                ClientAvatar = o.Client.PhotoThumbUrl,
                StartTimeUtc = o.StartTimeUtc,
                EndTimeUtc = o.EndTimeUtc,
                CityId = o.CityId,

                FullAddress = o.FullAddress,
                Latitude = o.Location != null ? o.Location.Y : (double?)null,
                Longitude = o.Location != null ? o.Location.X : (double?)null,
                CustomerBudget = o.CustomerBudget,
                Comment = o.Comment,
                Status = o.Status.ToString(),
                CreatedAt = o.CreatedAt
            })
            .ToListAsync();

        return Ok(feed);
    }
    [HttpPost("apply")]
    public async Task<IActionResult> ApplyToOrder([FromBody] ApplyToPublicOrderDto dto)
    {
        return Ok(await _publicOrderService.ApplyToOrderAsync(CurrentUserId, dto));
    }

    #endregion
}
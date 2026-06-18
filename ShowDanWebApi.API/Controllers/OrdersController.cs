using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShowDanWebApi.API.Service;
using ShowDanWebApi.Core.DTO;
using ShowDanWebApi.Core.Entities.Users;
using ShowDanWebApi.Data;
using static ShowDanWebApi.API.Controllers.AuthController;

namespace ShowDanWebApi.API.Controllers;

[Authorize]
[Route("api/orders")]
public class OrdersController : BaseController
{
    private readonly AppDbContext _context;
    private readonly IOrderPricingService _pricingService;
    private readonly IOrderService _orderService;

    public OrdersController(AppDbContext context, IOrderPricingService pricingService, IOrderService orderService) =>
        (_context, _pricingService, _orderService) = (context, pricingService, orderService);

    [HttpPost("estimate")]
    public async Task<IActionResult> EstimateOrderPrice([FromBody] EstimatePriceRequestDto dto)
    {
        if (dto.StartTimeUtc >= dto.EndTimeUtc) return BadRequest();
        return Ok(await _pricingService.CalculateOrderPriceAsync(dto));
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequestDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var startUtc = dto.StartTimeUtc.ToUniversalTime();
        var endUtc = dto.EndTimeUtc.ToUniversalTime();

        if (startUtc >= endUtc || startUtc < DateTime.UtcNow)
            return BadRequest(new { error = "Невалидный интервал времени заказа." });

        dto.StartTimeUtc = startUtc;
        dto.EndTimeUtc = endUtc;

        var strategy = _context.Database.CreateExecutionStrategy();
        object orderResult = null!;
        await strategy.ExecuteAsync(async () =>
        {
            orderResult = await _orderService.CreateOrderAsync(CurrentUserId, dto);
        });

        return Ok(orderResult);
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> ConfirmOrder([FromBody] ConfirmOrderRequestDto dto)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        object result = null!;

        await strategy.ExecuteAsync(async () =>
        {
            result = await _orderService.ConfirmOrderAsync(CurrentUserId, dto.OrderId);
        });

        return Ok(result);
    }

    [HttpPost("cancel")]
    public async Task<IActionResult> CancelOrder([FromBody] CancelOrderRequestDto dto)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        object result = null!;

        await strategy.ExecuteAsync(async () =>
        {
            result = await _orderService.CancelOrderAsync(CurrentUserId, dto);
        });

        return Ok(result);
    }

    [HttpPost("my-orders")]
    public async Task<IActionResult> GetMyOrders([FromBody] GetOrdersRequestDto dto)
    {
        int limit = Math.Min(dto.Limit, 50);
        bool isClient = CurrentUserRole == UserRoles.Client;
        var query = _context.Orders.AsNoTracking();
        query = isClient
            ? query.Where(o => o.ClientId == CurrentUserId)
            : query.Where(o => o.PerformerId == CurrentUserId);

        if (dto.IsHistory)
        {
            var halfYearAgo = DateTime.UtcNow.AddMonths(-6);
            query = query.Where(o => (o.Status == OrderStatus.Completed || o.Status == OrderStatus.Cancelled) && o.CreatedAt >= halfYearAgo);
        }
        else
        {
            query = query.Where(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled);
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip(dto.Offset)
            .Take(limit)
            .Select(o => new OrderShortResponseDto
            {
                OrderId = o.OrderId,
                DisplayName = isClient
                    ? (o.Service.StageName ?? "Артист")
                    : $"{o.Client.FirstName} {o.Client.LastName}",

                DisplayAvatar = isClient
                    ? o.Performer.User.PhotoThumbUrl
                    : o.Client.PhotoThumbUrl,

                StartTimeUtc = o.StartTimeUtc,
                EndTimeUtc = o.EndTimeUtc,
                CityId = o.CityId,

                TotalPrice = o.PerformancePrice + o.TravelPrice,
                CurrencyCode = o.CurrencyCode,
                Status = o.Status.ToString(),
                CreatedAt = o.CreatedAt
            })
            .ToListAsync();

        return Ok(orders);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOrderById(int id)
    {
        var order = await _context.Orders.AsNoTracking()
            .Where(o => o.OrderId == id && (o.ClientId == CurrentUserId || o.PerformerId == CurrentUserId))
            .Select(o => new OrderResponseDto
            {
                OrderId = o.OrderId,
                ClientId = o.ClientId,
                ClientName = $"{o.Client.FirstName} {o.Client.LastName}",
                ClientAvatar = o.Client.PhotoThumbUrl,
                PerformerId = o.PerformerId,
                PerformerNickname = o.Service.StageName ?? "Артист",
                PerformerAvatar = o.Performer.User.PhotoThumbUrl,
                ServiceId = o.ServiceId,
                StartTimeUtc = o.StartTimeUtc,
                EndTimeUtc = o.EndTimeUtc,
                CityId = o.CityId,
                FullAddress = o.FullAddress,
                Latitude = o.Location != null ? o.Location.Y : null,
                Longitude = o.Location != null ? o.Location.X : null,
                PerformancePrice = o.PerformancePrice,
                TravelPrice = o.TravelPrice,
                TotalPrice = o.PerformancePrice + o.TravelPrice,
                CurrencyCode = o.CurrencyCode,
                Status = o.Status.ToString(),
                ClientComment = o.ClientComment,
                CancellationReason = o.CancellationReason,
                CancelledBy = (int?)o.CancelledBy,
                CreatedAt = o.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (order == null) return NotFound();
        return Ok(order);
    }
}
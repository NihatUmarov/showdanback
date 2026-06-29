using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using ShowDanWebApi.Core.DTO;
using ShowDanWebApi.Core.Entities.Users;
using ShowDanWebApi.Data;

namespace ShowDanWebApi.API.Service;

public class OrderPricingService : IOrderPricingService
{
    private readonly AppDbContext _context;
    private readonly IMapService _mapService;
    private const decimal PricePerKilometer = 2.0m;

    public OrderPricingService(AppDbContext context, IMapService mapService) =>
        (_context, _mapService) = (context, mapService);

    public async Task<PriceEstimationResultDto> CalculateOrderPriceAsync(EstimatePriceRequestDto dto, int? preCalculatedCityId = null)
    {
        

        double durationHours = (dto.EndTimeUtc - dto.StartTimeUtc).TotalHours;
        if (durationHours <= 0) durationHours = 1;

       

        return new PriceEstimationResultDto { PerformancePrice = Math.Round(performancePrice, 2), TravelPrice = travelPrice, TotalPrice = Math.Round(performancePrice + travelPrice, 2), CurrencyCode = "USD" };
    }
}

public class OrderService : IOrderService
{
     
        if (payload.DeleteVideoUrls?.Count > 0)
        {
            service.VideoPersonal!.RemoveAll(v => payload.DeleteVideoUrls.Contains(v.Url));
        }
    public async Task<object> CancelOrderAsync(int userId, CancelOrderRequestDto dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == dto.OrderId);
        if (order == null) throw new KeyNotFoundException();


        return new { id = order.OrderId };
    }
    }
}
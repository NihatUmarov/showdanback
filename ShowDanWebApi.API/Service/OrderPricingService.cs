using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using ShowDanWebApi.Core.DTO;
using ShowDanWebApi.Core.Entities.Users;
using ShowDanWebApi.Data;

namespace ShowDanWebApi.API.Service;

public interface IOrderService
{
    Task<object> CreateOrderAsync(int clientId, CreateOrderRequestDto dto);
    Task<object> ConfirmOrderAsync(int performerId, int orderId);
    Task<object> CancelOrderAsync(int userId, CancelOrderRequestDto dto);
}

public interface IOrderPricingService
{
    // ИСПРАВЛЕНО: Строгий интовый контракт для калькуляции стоимости
    Task<PriceEstimationResultDto> CalculateOrderPriceAsync(EstimatePriceRequestDto dto, int? preCalculatedCityId = null);
}

public class OrderPricingService : IOrderPricingService
{
    private readonly AppDbContext _context;
    private readonly IMapService _mapService;
    private const decimal PricePerKilometer = 2.0m;

    public OrderPricingService(AppDbContext context, IMapService mapService) =>
        (_context, _mapService) = (context, mapService);

    public async Task<PriceEstimationResultDto> CalculateOrderPriceAsync(EstimatePriceRequestDto dto, int? preCalculatedCityId = null)
    {
        var service = await _context.PerformerServices.AsNoTracking().FirstOrDefaultAsync(s => s.ServiceId == dto.ServiceId);
        if (service == null) throw new KeyNotFoundException("Услуга исполнителя не найдена.");

        if (!dto.Latitude.HasValue || !dto.Longitude.HasValue) throw new ArgumentException("Координаты обязательны.");

        int performerId = service.PerformerId;
        var targetDate = DateTime.SpecifyKind(dto.StartTimeUtc.Date, DateTimeKind.Utc);

        // ИСПРАВЛЕНО: Определение города переведено на int
        int orderCityId = preCalculatedCityId ?? await _mapService.IdentifyCityIdAsync(_context, dto.Latitude.Value, dto.Longitude.Value);
        int artistCityId = 0;
        Point? artistLocation = null;

        var availabilityOverride = await _context.PerformerAvailabilities
            .AsNoTracking()
            .Where(a => a.PerformerId == performerId && a.StartTimeUtc.Date == targetDate)
            .Select(a => new { a.CityId, a.OverrideLocation })
            .FirstOrDefaultAsync();

        if (availabilityOverride != null && availabilityOverride.CityId > 0)
        {
            artistCityId = availabilityOverride.CityId;
            artistLocation = availabilityOverride.OverrideLocation;
        }
        else
        {
            var activeOrderSameDay = await _context.Orders
                .AsNoTracking()
                .Where(o => o.PerformerId == performerId && o.StartTimeUtc.Date == targetDate && (o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Paid || o.Status == OrderStatus.InProgress))
                .Select(o => new { o.CityId, o.Location })
                .FirstOrDefaultAsync();

            if (activeOrderSameDay != null)
            {
                artistCityId = activeOrderSameDay.CityId;
                artistLocation = activeOrderSameDay.Location;
            }
            else
            {
                var basePerformerProfile = await _context.Performers
                    .AsNoTracking()
                    .Where(p => p.UserId == performerId)
                    .Select(p => new { p.CityId, p.Location })
                    .FirstOrDefaultAsync();

                if (basePerformerProfile == null) throw new KeyNotFoundException("Профиль исполнителя не найден.");

                artistCityId = basePerformerProfile.CityId;
                artistLocation = basePerformerProfile.Location;
            }
        }

        double durationHours = (dto.EndTimeUtc - dto.StartTimeUtc).TotalHours;
        if (durationHours <= 0) durationHours = 1;

        decimal performancePrice = 0;
        int roundedHours = (int)Math.Ceiling(durationHours);
        var matchedPackage = service.PricePacks?.FirstOrDefault(p => p.Hours == roundedHours);

        if (matchedPackage != null) performancePrice = matchedPackage.Amount;
        else if (service.PriceHour.HasValue) performancePrice = service.PriceHour.Value * (decimal)durationHours;
        else performancePrice = service.PricePacks?.FirstOrDefault()?.Amount ?? 0;

        decimal travelPrice = 0;

        // ИСПРАВЛЕНО: Нативное, сверхбыстрое сравнение интов artistCityId != orderCityId
        if (artistCityId != orderCityId && artistLocation != null)
        {
            var routeResult = _mapService.CalculateRoute((float)artistLocation.Y, (float)artistLocation.X, (float)dto.Latitude.Value, (float)dto.Longitude.Value);
            if (routeResult != null)
            {
                travelPrice = Math.Round((decimal)(routeResult.TotalDistance / 1000.0) * PricePerKilometer, 2);
            }
        }

        return new PriceEstimationResultDto { PerformancePrice = Math.Round(performancePrice, 2), TravelPrice = travelPrice, TotalPrice = Math.Round(performancePrice + travelPrice, 2), CurrencyCode = "USD" };
    }
}

public class OrderService : IOrderService
{
    private readonly AppDbContext _context;
    private readonly IOrderPricingService _pricingService;
    private readonly IMapService _mapService;
    private readonly GeometryFactory _geometryFactory = new(new PrecisionModel(), 4326);

    public OrderService(AppDbContext context, IOrderPricingService pricingService, IMapService mapService) =>
        (_context, _pricingService, _mapService) = (context, pricingService, mapService);

    public async Task<object> CreateOrderAsync(int clientId, CreateOrderRequestDto dto)
    {
        if (!dto.Latitude.HasValue || !dto.Longitude.HasValue) throw new ArgumentException("Укажите локацию.");

        using var transaction = await _context.Database.BeginTransactionAsync();

        var service = await _context.PerformerServices.AsNoTracking().FirstOrDefaultAsync(s => s.ServiceId == dto.ServiceId);
        if (service == null) throw new KeyNotFoundException();

        bool hasHardBooking = await _context.Orders.AnyAsync(o =>
            o.PerformerId == service.PerformerId &&
            (o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Paid || o.Status == OrderStatus.InProgress) &&
            o.StartTimeUtc < dto.EndTimeUtc && o.EndTimeUtc > dto.StartTimeUtc);

        if (hasHardBooking) throw new ApplicationException("Это время уже забронировано.");

        int orderCityId = await _mapService.IdentifyCityIdAsync(_context, dto.Latitude.Value, dto.Longitude.Value);
        var calculatedPrices = await _pricingService.CalculateOrderPriceAsync(new EstimatePriceRequestDto { ServiceId = dto.ServiceId, StartTimeUtc = dto.StartTimeUtc, EndTimeUtc = dto.EndTimeUtc, Latitude = dto.Latitude, Longitude = dto.Longitude }, orderCityId);

        var orderLocation = _geometryFactory.CreatePoint(new Coordinate(dto.Longitude.Value, dto.Latitude.Value));

        string dbCurrencyCode = await _context.Currencies.Where(c => c.Code == "USD").Select(c => c.Code).FirstOrDefaultAsync()
                                ?? await _context.Currencies.Select(c => c.Code).FirstOrDefaultAsync() ?? "USD";

        if (!await _context.Cities.AnyAsync(c => c.CityId == orderCityId))
        {
            orderCityId = await _context.Performers.Where(p => p.UserId == service.PerformerId).Select(p => p.CityId).FirstOrDefaultAsync();
        }

        var osmAddress = await _mapService.GetAddressByCoordinatesAsync(_context, dto.Latitude.Value, dto.Longitude.Value, maxDistanceMeters: 300);

        string finalAddress;
        if (osmAddress != null)
        {
            string street = osmAddress.Street?.Ru ?? osmAddress.Street?.Uz ?? string.Empty;
            string house = osmAddress.HouseNumber ?? string.Empty;
            string city = osmAddress.City?.Ru ?? osmAddress.City?.Uz ?? string.Empty;

            var addressParts = new List<string> { city, street, house }.Where(s => !string.IsNullOrWhiteSpace(s));
            finalAddress = string.Join(", ", addressParts);
        }
        else
        {
            finalAddress = $"Координаты: {dto.Latitude.Value.ToString("F6")}, {dto.Longitude.Value.ToString("F6")} (ID: {orderCityId})";
        }

        var order = new Orders
        {
            ClientId = clientId,
            PerformerId = service.PerformerId,
            ServiceId = dto.ServiceId,
            StartTimeUtc = dto.StartTimeUtc,
            EndTimeUtc = dto.EndTimeUtc,
            CityId = orderCityId,
            FullAddress = finalAddress,
            Location = orderLocation,
            PerformancePrice = calculatedPrices.PerformancePrice,
            TravelPrice = calculatedPrices.TravelPrice,
            CurrencyCode = dbCurrencyCode,
            Status = OrderStatus.Created,
            ClientComment = dto.ClientComment,
            CreatedAt = DateTime.UtcNow
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        _context.PerformerAvailabilities.Add(new PerformerAvailability
        {
            PerformerId = service.PerformerId,
            StartTimeUtc = dto.StartTimeUtc,
            EndTimeUtc = dto.EndTimeUtc,
            CityId = orderCityId,
            OverrideLocation = orderLocation,
            Status = BusyStatus.Tentative,
            OrderId = order.OrderId,
            Note = $"#{order.OrderId}"
        });

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return new CreateOrderResponseDto { OrderId = order.OrderId, TotalPrice = (order.PerformancePrice + order.TravelPrice) };
    }

    public async Task<object> ConfirmOrderAsync(int performerId, int orderId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
        if (order == null) throw new KeyNotFoundException();
        if (order.PerformerId != performerId) throw new UnauthorizedAccessException();
        if (order.Status != OrderStatus.Created) throw new InvalidOperationException();

        bool hasConfirmedConflict = await _context.Orders.AnyAsync(o =>
            o.PerformerId == performerId && o.OrderId != orderId &&
            (o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Paid || o.Status == OrderStatus.InProgress) &&
            o.StartTimeUtc < order.EndTimeUtc && o.EndTimeUtc > order.StartTimeUtc);

        if (hasConfirmedConflict) throw new InvalidOperationException("Конфликт расписания.");

        order.Status = OrderStatus.Confirmed;
        order.UpdatedAt = DateTime.UtcNow;

        if (await _context.PerformerAvailabilities.FirstOrDefaultAsync(a => a.OrderId == orderId) is { } availability)
            availability.Status = BusyStatus.Booked;

        var overlappingOrders = await _context.Orders
            .Where(o => o.PerformerId == performerId && o.OrderId != orderId && o.Status == OrderStatus.Created && o.StartTimeUtc < order.EndTimeUtc && o.EndTimeUtc > order.StartTimeUtc)
            .ToListAsync();

        if (overlappingOrders.Count > 0)
        {
            var overlappingOrderIds = overlappingOrders.Select(o => o.OrderId).ToList();

            overlappingOrders.ForEach(o =>
            {
                o.Status = OrderStatus.Cancelled;
                o.CancelledBy = CancelledByType.System;
                o.UpdatedAt = DateTime.UtcNow;
            });

            var availabilitiesToRemove = await _context.PerformerAvailabilities
                .Where(a => a.OrderId.HasValue && overlappingOrderIds.Contains(a.OrderId.Value))
                .ToListAsync();

            _context.PerformerAvailabilities.RemoveRange(availabilitiesToRemove);
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return new { id = order.OrderId };
    }

    public async Task<object> CancelOrderAsync(int userId, CancelOrderRequestDto dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == dto.OrderId);
        if (order == null) throw new KeyNotFoundException();

        if (order.ClientId != userId && order.PerformerId != userId) throw new UnauthorizedAccessException();
        if (order.Status is OrderStatus.Paid or OrderStatus.InProgress or OrderStatus.Completed) throw new InvalidOperationException("Нельзя отменить выполняющийся заказ.");

        order.CancelledBy = order.ClientId == userId ? CancelledByType.Client : CancelledByType.Performer;
        order.Status = OrderStatus.Cancelled;
        order.CancellationReason = dto.Reason;
        order.UpdatedAt = DateTime.UtcNow;

        if (await _context.PerformerAvailabilities.FirstOrDefaultAsync(a => a.OrderId == dto.OrderId) is { } availability)
            _context.PerformerAvailabilities.Remove(availability);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return new { id = order.OrderId };
    }
}
using Microsoft.EntityFrameworkCore;
using ShowDanWebApi.Core.DTO;
using ShowDanWebApi.Core.Entities.Users;
using ShowDanWebApi.Data;

namespace ShowDanWebApi.API.Service;

public interface IPublicOrderService
{
    Task<object> CreatePublicOrderAsync(int clientId, CreatePublicOrderDto dto);
    Task<object> ApplyToOrderAsync(int performerId, ApplyToPublicOrderDto dto);
    Task<object> AcceptPerformerAsync(int clientId, int publicOrderId, int applicationId);
}


public class PublicOrderService : IPublicOrderService
{
    private readonly AppDbContext _context;
    private readonly IOrderPricingService _pricingService;
    private readonly IOrderService _orderService;
    private readonly IMapService _mapService;

    public PublicOrderService(AppDbContext context, IOrderPricingService pricingService, IOrderService orderService, IMapService mapService)
    {
        _context = context;
        _pricingService = pricingService;
        _orderService = orderService;
        _mapService = mapService;
    }

    public async Task<object> CreatePublicOrderAsync(int clientId, CreatePublicOrderDto dto)
    {
        var startUtc = dto.StartTimeUtc.ToUniversalTime();
        var endUtc = dto.EndTimeUtc.ToUniversalTime();

        if (startUtc >= endUtc || startUtc < DateTime.UtcNow)
            throw new ArgumentException("Некорректные временные рамки.");

        if (!dto.Latitude.HasValue || !dto.Longitude.HasValue)
            throw new ArgumentException("Координаты места проведения обязательны.");

        // Получаем чистый int ID города из обновленного MapService
        int detectedCityId = await _mapService.IdentifyCityIdAsync(_context, dto.Latitude.Value, dto.Longitude.Value);

        // Нативная и супербыстрая проверка по первичному ключу в БД
        if (!await _context.Cities.AnyAsync(c => c.CityId == detectedCityId))
        {
            detectedCityId = 1; // Числовой ID дефолтного города (Ташкент) взамен старой строки "tash"
        }

        var geometryFactory = new NetTopologySuite.Geometries.GeometryFactory(new NetTopologySuite.Geometries.PrecisionModel(), 4326);
        var orderLocation = geometryFactory.CreatePoint(new NetTopologySuite.Geometries.Coordinate(dto.Longitude.Value, dto.Latitude.Value));

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
            finalAddress = $"Координаты: {dto.Latitude.Value.ToString("F6")}, {dto.Longitude.Value.ToString("F6")} (ID: {detectedCityId})";
        }

        var publicOrder = new PublicOrders
        {
            ClientId = clientId,
            StartTimeUtc = startUtc,
            EndTimeUtc = endUtc,
            CityId = detectedCityId, // Записываем чистый int в сущность PublicOrders
            FullAddress = finalAddress,
            Location = orderLocation,
            CustomerBudget = dto.CustomerBudget,
            Comment = dto.Comment,
            Status = PublicOrderStatus.Open,
            CreatedAt = DateTime.UtcNow
        };

        _context.Set<PublicOrders>().Add(publicOrder);
        await _context.SaveChangesAsync();

        return new { publicOrderId = publicOrder.PublicOrderId, detectedAddress = finalAddress };
    }

    public async Task<object> ApplyToOrderAsync(int performerId, ApplyToPublicOrderDto dto)
    {
        var publicOrder = await _context.Set<PublicOrders>()
            .FirstOrDefaultAsync(o => o.PublicOrderId == dto.PublicOrderId && o.Status == PublicOrderStatus.Open);

        if (publicOrder == null) throw new KeyNotFoundException("Заказ не найден или уже закрыт.");

        bool isBusy = await _context.Orders.AnyAsync(o =>
            o.PerformerId == performerId &&
            (o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Paid || o.Status == OrderStatus.InProgress) &&
            o.StartTimeUtc < publicOrder.EndTimeUtc && o.EndTimeUtc > publicOrder.StartTimeUtc);

        if (isBusy) throw new InvalidOperationException("Вы заняты в это время.");

        bool alreadyApplied = await _context.Set<PublicOrderApplications>()
            .AnyAsync(a => a.PublicOrderId == dto.PublicOrderId && a.PerformerId == performerId);
        if (alreadyApplied) throw new InvalidOperationException("Вы уже откликнулись на этот заказ.");

        // Передаем интовый publicOrder.CityId напрямую в расчет цены
        var estimation = await _pricingService.CalculateOrderPriceAsync(new EstimatePriceRequestDto
        {
            ServiceId = dto.ServiceId,
            StartTimeUtc = publicOrder.StartTimeUtc,
            EndTimeUtc = publicOrder.EndTimeUtc,
            Latitude = publicOrder.Location?.Y,
            Longitude = publicOrder.Location?.X
        }, publicOrder.CityId);

        var application = new PublicOrderApplications
        {
            PublicOrderId = dto.PublicOrderId,
            PerformerId = performerId,
            ServiceId = dto.ServiceId,
            BidPrice = estimation.PerformancePrice,
            TravelPrice = estimation.TravelPrice,
            CoverLetter = dto.CoverLetter,
            Status = ApplicationStatus.Pending
        };

        _context.Set<PublicOrderApplications>().Add(application);
        await _context.SaveChangesAsync();

        return new { ApplicationId = application.ApplicationId };
    }

    public async Task<object> AcceptPerformerAsync(int clientId, int publicOrderId, int applicationId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        var publicOrder = await _context.Set<PublicOrders>()
            .Include(o => o.Applications)
            .FirstOrDefaultAsync(o => o.PublicOrderId == publicOrderId && o.ClientId == clientId);

        if (publicOrder == null || publicOrder.Status != PublicOrderStatus.Open)
            throw new InvalidOperationException("Заказ недоступен для обработки.");

        var targetApplication = publicOrder.Applications.FirstOrDefault(a => a.ApplicationId == applicationId);
        if (targetApplication == null || targetApplication.Status != ApplicationStatus.Pending)
            throw new KeyNotFoundException("Отклик не найден.");

        publicOrder.Status = PublicOrderStatus.Accepted;
        targetApplication.Status = ApplicationStatus.Accepted;

        publicOrder.Applications
            .Where(a => a.ApplicationId != applicationId)
            .ToList()
            .ForEach(a => a.Status = ApplicationStatus.Rejected);

        var standardOrderDto = new CreateOrderRequestDto
        {
            ServiceId = targetApplication.ServiceId,
            StartTimeUtc = publicOrder.StartTimeUtc,
            EndTimeUtc = publicOrder.EndTimeUtc,
            Latitude = publicOrder.Location?.Y,
            Longitude = publicOrder.Location?.X,
            ClientComment = $"[Из Стола Заказов] {publicOrder.Comment}"
        };

        var orderResult = await _orderService.CreateOrderAsync(clientId, standardOrderDto);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return orderResult;
    }
}
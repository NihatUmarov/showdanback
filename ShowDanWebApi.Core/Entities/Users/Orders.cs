using NetTopologySuite.Geometries;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShowDanWebApi.Core.Entities.Users;

public class Orders
{
    [Key] public int OrderId { get; set; }

    public int ClientId { get; set; }
    [ForeignKey(nameof(ClientId))]
    public virtual Users Client { get; set; } = null!;

    public int PerformerId { get; set; }
    [ForeignKey(nameof(PerformerId))]
    public virtual Performers Performer { get; set; } = null!;

    public int ServiceId { get; set; }
    [ForeignKey(nameof(ServiceId))]
    public virtual PerformerServices Service { get; set; } = null!;

    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }

    public int CityId { get; set; } [ForeignKey(nameof(CityId))] public virtual Cities City { get; set; } = null!;

    public string? FullAddress { get; set; }
    public Point? Location { get; set; }

    [Column(TypeName = "decimal(18,2)")] public decimal PerformancePrice { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal TravelPrice { get; set; } = 0;
    [NotMapped] public decimal TotalPrice => PerformancePrice + TravelPrice;

    [Required][StringLength(3)] public string CurrencyCode { get; set; } = "USD";
    [ForeignKey(nameof(CurrencyCode))] public virtual Currencies Currency { get; set; } = null!;

    public OrderStatus Status { get; set; } = OrderStatus.Created;

    [StringLength(1000)] public string? ClientComment { get; set; }
    [StringLength(500)] public string? CancellationReason { get; set; }

    public CancelledByType? CancelledBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public enum CancelledByType
{
    Client = 1,
    Performer = 2,
    System = 3,  // Авто-отмена, если не ответил вовремя
    Admin = 4    // Диспут / Поддержка
}

public enum OrderStatus
{
    Created = 1,      // Заказ создан клиентом, ожидает подтверждения от артиста
    Confirmed = 2,    // Артист подтвердил, что готов выступить (ожидаем оплату от клиента)
    Paid = 3,         // Клиент оплатил (деньги захолдированы системой)
    InProgress = 4,   // Мероприятие идет прямо сейчас
    Completed = 5,    // Успешно завершено (деньги уходят артисту)
    Cancelled = 6     // Отменен (клиентом, артистом или администрацией)
}
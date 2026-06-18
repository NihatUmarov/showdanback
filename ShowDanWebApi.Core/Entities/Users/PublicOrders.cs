using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace ShowDanWebApi.Core.Entities.Users;

public enum PublicOrderStatus { Open = 1, Accepted = 2, Cancelled = 3 }
public enum ApplicationStatus { Pending = 1, Accepted = 2, Rejected = 3 }

public class PublicOrders
{
    [Key] public int PublicOrderId { get; set; }

    public int ClientId { get; set; }
    [ForeignKey(nameof(ClientId))] public virtual Users Client { get; set; } = null!;

    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public int CityId { get; set; } [ForeignKey(nameof(CityId))] public virtual Cities City { get; set; } = null!;

    public string? FullAddress { get; set; }
    public Point? Location { get; set; }

    [Column(TypeName = "decimal(18,2)")] public decimal? CustomerBudget { get; set; }
    [StringLength(1000)] public string? Comment { get; set; }

    public PublicOrderStatus Status { get; set; } = PublicOrderStatus.Open;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<PublicOrderApplications> Applications { get; set; } = new List<PublicOrderApplications>();
}

public class PublicOrderApplications
{
    [Key] public int ApplicationId { get; set; }

    public int PublicOrderId { get; set; }
    [ForeignKey(nameof(PublicOrderId))] public virtual PublicOrders PublicOrder { get; set; } = null!;

    public int PerformerId { get; set; }
    [ForeignKey(nameof(PerformerId))] public virtual Performers Performer { get; set; } = null!;

    public int ServiceId { get; set; }
    [ForeignKey(nameof(ServiceId))] public virtual PerformerServices Service { get; set; } = null!;
    [Column(TypeName = "decimal(18,2)")] public decimal BidPrice { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal TravelPrice { get; set; }

    [StringLength(500)] public string? CoverLetter { get; set; }

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace ShowDanWebApi.Core.Entities.Users;

public enum PublicOrderStatus { Open = 1, Accepted = 2, Cancelled = 3 }
public enum ApplicationStatus { Pending = 1, Accepted = 2, Rejected = 3 }

public class PublicOrders
{
    [Key] public int PublicOrderId { get; set; }
    [ForeignKey(nameof(ServiceId))] public virtual PerformerServices Service { get; set; } = null!;
    [Column(TypeName = "decimal(18,2)")] public decimal BidPrice { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal TravelPrice { get; set; }
ws
    [StringLength(500)] public string? CoverLetter { geet; set; }

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
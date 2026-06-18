
using NetTopologySuite.Geometries;
using ShowDanWebApi.Core.Entities;
using ShowDanWebApi.Core.Entities.Users;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class PerformerAvailability
{
    [Key] public long AvailabilityId { get; set; }

    public int PerformerId { get; set; }
    [ForeignKey(nameof(PerformerId))] public virtual Performers Performer { get; set; } = null!;

    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }

    public int CityId { get; set; } [ForeignKey(nameof(CityId))] public virtual Cities City { get; set; } = null!;
    public Point? OverrideLocation { get; set; }

    public BusyStatus Status { get; set; }

    public int? OrderId { get; set; }
    [ForeignKey(nameof(OrderId))] public virtual Orders? Order { get; set; }

    public string? Note { get; set; }
}

public enum BusyStatus { Booked = 1, External = 2, Personal = 3, Tentative = 4, Block = 5 }

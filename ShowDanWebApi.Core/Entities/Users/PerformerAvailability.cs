
using NetTopologySuite.Geometries;
using ShowDanWebApi.Core.Entities;
using ShowDanWebApi.Core.Entities.Users;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class PerformerAvailability
{
    [Key] public long AvailabilityId { get; set; }

    public int PerformerId { get; set; }
}

e
using Microsoft.EntityFrameworkCore;
using ShowDanWebApi.Core.Entities;
using ShowDanWebApi.Core.Entities.Chat;
using ShowDanWebApi.Core.Entities.News;
using ShowDanWebApi.Core.Entities.References;
using ShowDanWebApi.Core.Entities.Settings;
using ShowDanWebApi.Core.Entities.Users;

namespace ShowDanWebApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Countries> Countries { get; set; }
    public DbSet<Cities> Cities { get; set; }
    public DbSet<Orders> Orders { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("ShowData");
        modelBuilder.HasPostgresExtension("pg_trgm");

        modelBuilder.Entity<ServiceGenreCodes>().HasKey(pg => new { pg.ServiceId, pg.GenreCodeId });
        modelBuilder.Entity<ServiceTypeCodes>().HasKey(pt => new { pt.ServiceId, pt.TypeCodeId });
        modelBuilder.Entity<ServiceExtraCodes>().HasKey(se => new { se.ServiceId, se.ExtraCodeId });
    }
}
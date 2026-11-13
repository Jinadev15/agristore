using AgriStore.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AgriStore.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext(options)
{
    public DbSet<CropProfile> Crops => Set<CropProfile>();
    public DbSet<StorageUnit> StorageUnits => Set<StorageUnit>();
    public DbSet<StockLot> StockLots => Set<StockLot>();
    public DbSet<SensorReading> Readings => Set<SensorReading>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<CropProfile>().HasIndex(c => c.Name).IsUnique();

        // Deleting a unit or crop must not silently wipe farmers' stock records.
        builder.Entity<StockLot>()
            .HasOne(l => l.StorageUnit).WithMany(u => u.Lots)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StockLot>()
            .HasOne(l => l.Crop).WithMany(c => c.Lots)
            .OnDelete(DeleteBehavior.Restrict);

        // Readings belong to the unit; they go when the unit goes.
        builder.Entity<SensorReading>()
            .HasOne(r => r.StorageUnit).WithMany(u => u.Readings)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<SensorReading>().HasIndex(r => new { r.StorageUnitId, r.RecordedAt });
    }
}

using Microsoft.EntityFrameworkCore;
using ParcelTracking.Api.Domain;

namespace ParcelTracking.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Parcel> Parcels => Set<Parcel>();
    public DbSet<ParcelEvent> ParcelEvents => Set<ParcelEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Parcel>(parcel =>
        {
            parcel.HasIndex(p => p.TrackingNumber).IsUnique();
            parcel.Property(p => p.Status).HasConversion<string>();
            parcel.HasOne<Warehouse>().WithMany().HasForeignKey(p => p.WarehouseId);
        });

        modelBuilder.Entity<ParcelEvent>(parcelEvent =>
        {
            parcelEvent.HasIndex(e => e.EventId).IsUnique();
            parcelEvent.HasIndex(e => e.ParcelId);
            parcelEvent.Property(e => e.NewStatus).HasConversion<string>();
            parcelEvent.HasOne<Parcel>().WithMany().HasForeignKey(e => e.ParcelId);
        });

        modelBuilder.Entity<Warehouse>().HasData(new Warehouse { Id = 1, Name = "Main Warehouse" });
    }
}

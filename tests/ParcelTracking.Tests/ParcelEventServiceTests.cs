using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ParcelTracking.Api.Data;
using ParcelTracking.Api.Domain;
using ParcelTracking.Api.Services;

namespace ParcelTracking.Tests;

// Real SQLite (in memory) instead of EF's InMemory provider:
// only a relational database enforces the unique index on EventId.
public class ParcelEventServiceTests : IDisposable
{
    private static readonly DateTime OccurredAt = new(2026, 10, 25, 9, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly ParcelEventService _service;
    private readonly int _parcelId;

    public ParcelEventServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open(); // the in-memory database lives as long as this connection

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        var parcel = new Parcel { WarehouseId = 1, TrackingNumber = "PT0000000001", BinCode = "A" };
        var order = new Order { CustomerName = "Test Customer", CreatedAtUtc = OccurredAt, Parcels = { parcel } };
        _db.Orders.Add(order);
        _db.SaveChanges();
        _parcelId = parcel.Id;

        _service = new ParcelEventService(_db);
    }

    [Fact]
    public async Task NewEvent_IsApplied()
    {
        var result = await _service.ApplyAsync(_parcelId, "evt-1", ParcelStatus.Picked, OccurredAt);

        Assert.Equal(EventOutcome.Applied, result.Outcome);
        Assert.Equal(ParcelStatus.Picked, result.Parcel!.Status);
        Assert.Equal(1, await _db.ParcelEvents.CountAsync());
    }

    [Fact]
    public async Task SameEventTwice_ChangesStateOnce()
    {
        await _service.ApplyAsync(_parcelId, "evt-1", ParcelStatus.Picked, OccurredAt);
        var second = await _service.ApplyAsync(_parcelId, "evt-1", ParcelStatus.Picked, OccurredAt);

        Assert.Equal(EventOutcome.Duplicate, second.Outcome);
        Assert.Equal(ParcelStatus.Picked, second.Parcel!.Status);
        Assert.Equal(1, await _db.ParcelEvents.CountAsync());
    }

    [Fact]
    public async Task ReusedEventId_WithDifferentStatus_IsConflict()
    {
        await _service.ApplyAsync(_parcelId, "evt-1", ParcelStatus.Picked, OccurredAt);
        var result = await _service.ApplyAsync(_parcelId, "evt-1", ParcelStatus.Packed, OccurredAt);

        Assert.Equal(EventOutcome.EventIdConflict, result.Outcome);
        Assert.Equal(1, await _db.ParcelEvents.CountAsync());
    }

    [Fact]
    public async Task InvalidTransition_StoresNothing()
    {
        var result = await _service.ApplyAsync(_parcelId, "evt-1", ParcelStatus.Shipped, OccurredAt);

        Assert.Equal(EventOutcome.InvalidTransition, result.Outcome);
        Assert.Equal(0, await _db.ParcelEvents.CountAsync());
        var parcel = await _db.Parcels.FindAsync(_parcelId);
        Assert.Equal(ParcelStatus.Created, parcel!.Status);
    }

    [Fact]
    public async Task UnknownParcel_IsNotFound()
    {
        var result = await _service.ApplyAsync(999, "evt-1", ParcelStatus.Picked, OccurredAt);

        Assert.Equal(EventOutcome.ParcelNotFound, result.Outcome);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}

using Microsoft.EntityFrameworkCore;
using ParcelTracking.Api.Data;
using ParcelTracking.Api.Domain;
using ParcelTracking.Api.Routing;

namespace ParcelTracking.Api.Endpoints;

public record CreateOrderRequest(string? CustomerName);
public record CreateParcelRequest(int WarehouseId, string? BinCode);

public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/orders", async (CreateOrderRequest request, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.CustomerName))
                return ApiErrors.Validation("customerName", "customerName is required.");

            var order = new Order
            {
                CustomerName = request.CustomerName.Trim(),
                CreatedAtUtc = DateTime.UtcNow
            };
            db.Orders.Add(order);
            await db.SaveChangesAsync();

            return Results.Created($"/orders/{order.Id}", order);
        });

        app.MapGet("/orders/{id:int}", async (int id, AppDbContext db) =>
        {
            var order = await db.Orders
                .Include(o => o.Parcels)
                .FirstOrDefaultAsync(o => o.Id == id);

            return order is null ? Results.NotFound() : Results.Ok(order);
        });

        app.MapPost("/orders/{id:int}/parcels", async (int id, CreateParcelRequest request, AppDbContext db) =>
        {
            if (!await db.Orders.AnyAsync(o => o.Id == id))
                return Results.NotFound();

            if (!WarehouseLayouts.TryGet(request.WarehouseId, out var grid))
                return ApiErrors.Validation("warehouseId", "Unknown warehouse.");

            var bin = request.BinCode?.Trim().ToUpperInvariant();
            if (bin is null || bin.Length != 1 || !WarehouseLayouts.HasBin(grid, bin[0]))
                return ApiErrors.Validation("binCode", "binCode must be a bin letter that exists in this warehouse.");

            var parcel = new Parcel
            {
                OrderId = id,
                WarehouseId = request.WarehouseId,
                BinCode = bin,
                TrackingNumber = "PT" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant()
            };
            db.Parcels.Add(parcel);
            await db.SaveChangesAsync();

            return Results.Created($"/parcels/{parcel.Id}", parcel);
        });
    }
}

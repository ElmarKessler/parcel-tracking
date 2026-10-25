using Microsoft.EntityFrameworkCore;
using ParcelTracking.Api.Data;
using ParcelTracking.Api.Routing;

namespace ParcelTracking.Api.Endpoints;

public static class WarehouseEndpoints
{
    public static void MapWarehouseEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/warehouses/{id:int}/pick-route", async (int id, string? parcelIds, AppDbContext db) =>
        {
            if (!WarehouseLayouts.TryGet(id, out var grid))
                return Results.NotFound();

            var ids = ParseIds(parcelIds);
            if (ids is null || ids.Count == 0)
                return ApiErrors.Validation("parcelIds", "Provide comma-separated parcel ids, e.g. ?parcelIds=1,2,3.");

            var parcels = await db.Parcels
                .Where(p => ids.Contains(p.Id))
                .ToListAsync();

            var missing = ids.Except(parcels.Select(p => p.Id)).ToList();
            if (missing.Count > 0)
                return ApiErrors.Validation("parcelIds", $"Unknown parcel ids: {string.Join(", ", missing)}.");

            var otherWarehouse = parcels.Where(p => p.WarehouseId != id).Select(p => p.Id).ToList();
            if (otherWarehouse.Count > 0)
                return ApiErrors.Validation("parcelIds", $"Not in warehouse {id}: {string.Join(", ", otherWarehouse)}.");

            var stops = parcels.Select(p => new PickStop(p.Id, p.BinCode[0])).ToList();
            var result = PickRoutePlanner.Plan(grid, stops);

            return result.Route is null
                ? Results.Problem(statusCode: 422, title: "Unreachable bins",
                    detail: $"Parcels: {string.Join(", ", result.UnreachableParcelIds)}.")
                : Results.Ok(result.Route);
        });
    }

    private static List<int>? ParseIds(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var ids = new List<int>();
        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!int.TryParse(part, out var id))
                return null;
            ids.Add(id);
        }
        return ids.Distinct().ToList();
    }
}

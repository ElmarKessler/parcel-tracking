using Microsoft.EntityFrameworkCore;
using ParcelTracking.Api.Data;

namespace ParcelTracking.Api.Endpoints;

public static class ParcelEndpoints
{
    public static void MapParcelEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/parcels/{id:int}", async (int id, AppDbContext db) =>
        {
            var parcel = await db.Parcels.FindAsync(id);
            return parcel is null ? Results.NotFound() : Results.Ok(parcel);
        });

        app.MapGet("/parcels/{id:int}/history", async (int id, AppDbContext db) =>
        {
            if (!await db.Parcels.AnyAsync(p => p.Id == id))
                return Results.NotFound();

            var events = await db.ParcelEvents
                .Where(e => e.ParcelId == id)
                .OrderBy(e => e.Id)
                .ToListAsync();

            return Results.Ok(events);
        });
    }
}

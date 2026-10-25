using ParcelTracking.Api.Domain;
using ParcelTracking.Api.Services;

namespace ParcelTracking.Api.Endpoints;

public record ParcelEventRequest(string? EventId, ParcelStatus? NewStatus, DateTime? OccurredAt);

public static class ParcelEventEndpoints
{
    public static void MapParcelEventEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/parcels/{id:int}/events", async (int id, ParcelEventRequest request, ParcelEventService service) =>
        {
            if (string.IsNullOrWhiteSpace(request.EventId))
                return ApiErrors.Validation("eventId", "eventId is required.");
            if (request.NewStatus is null)
                return ApiErrors.Validation("newStatus", "newStatus is required.");
            if (request.OccurredAt is null)
                return ApiErrors.Validation("occurredAt", "occurredAt is required.");

            var result = await service.ApplyAsync(
                id, request.EventId, request.NewStatus.Value, request.OccurredAt.Value.ToUniversalTime());

            return result.Outcome switch
            {
                EventOutcome.Applied => Results.Created($"/parcels/{id}/history", result.Parcel),
                EventOutcome.Duplicate => Results.Ok(result.Parcel),
                EventOutcome.ParcelNotFound => Results.NotFound(),
                EventOutcome.InvalidTransition => Results.Problem(
                    statusCode: 409, title: "Invalid status transition", detail: result.Message),
                EventOutcome.EventIdConflict => Results.Problem(
                    statusCode: 409, title: "eventId already used for a different event", detail: result.Message),
                _ => Results.Problem(statusCode: 500)
            };
        });
    }
}

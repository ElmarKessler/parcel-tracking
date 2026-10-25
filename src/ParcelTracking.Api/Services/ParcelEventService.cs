using Microsoft.EntityFrameworkCore;
using ParcelTracking.Api.Data;
using ParcelTracking.Api.Domain;

namespace ParcelTracking.Api.Services;

public enum EventOutcome
{
    Applied,
    Duplicate,
    ParcelNotFound,
    InvalidTransition,
    EventIdConflict
}

public record EventResult(EventOutcome Outcome, Parcel? Parcel = null, string? Message = null);

public class ParcelEventService(AppDbContext db)
{
    public async Task<EventResult> ApplyAsync(int parcelId, string eventId, ParcelStatus newStatus, DateTime occurredAtUtc)
    {
        // 1. Already seen this eventId? Then it is a retry (or a misuse of the id).
        var existing = await db.ParcelEvents.SingleOrDefaultAsync(e => e.EventId == eventId);
        if (existing is not null)
            return await DuplicateOrConflictAsync(existing, parcelId, newStatus);

        // 2. Parcel must exist.
        var parcel = await db.Parcels.FindAsync(parcelId);
        if (parcel is null)
            return new EventResult(EventOutcome.ParcelNotFound);

        // 3. State machine decides. Nothing is stored on rejection.
        if (!ParcelStateMachine.CanTransition(parcel.Status, newStatus))
            return new EventResult(EventOutcome.InvalidTransition, parcel,
                $"Cannot change status from {parcel.Status} to {newStatus}.");

        // 4. Status change and event row in one SaveChanges call = one transaction.
        parcel.Status = newStatus;
        db.ParcelEvents.Add(new ParcelEvent
        {
            EventId = eventId,
            ParcelId = parcelId,
            NewStatus = newStatus,
            OccurredAtUtc = occurredAtUtc,
            ReceivedAtUtc = DateTime.UtcNow
        });

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Safety net: the unique index on EventId rejected the row,
            // so the same event was stored between step 1 and now.
            db.ChangeTracker.Clear();
            var stored = await db.ParcelEvents.SingleOrDefaultAsync(e => e.EventId == eventId);
            if (stored is null)
                throw;
            return await DuplicateOrConflictAsync(stored, parcelId, newStatus);
        }

        return new EventResult(EventOutcome.Applied, parcel);
    }

    private async Task<EventResult> DuplicateOrConflictAsync(ParcelEvent existing, int parcelId, ParcelStatus newStatus)
    {
        if (existing.ParcelId != parcelId || existing.NewStatus != newStatus)
            return new EventResult(EventOutcome.EventIdConflict, null,
                $"eventId '{existing.EventId}' was already used for parcel {existing.ParcelId} with status {existing.NewStatus}.");

        var parcel = await db.Parcels.FindAsync(parcelId);
        return new EventResult(EventOutcome.Duplicate, parcel);
    }
}

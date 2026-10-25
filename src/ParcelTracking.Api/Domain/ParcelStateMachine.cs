namespace ParcelTracking.Api.Domain;

public static class ParcelStateMachine
{
    // Explicit list of allowed transitions. A new status (e.g. Cancelled) means new entries here.
    private static readonly HashSet<(ParcelStatus From, ParcelStatus To)> Allowed = new()
    {
        (ParcelStatus.Created, ParcelStatus.Picked),
        (ParcelStatus.Picked, ParcelStatus.Packed),
        (ParcelStatus.Packed, ParcelStatus.Shipped),
        (ParcelStatus.Shipped, ParcelStatus.Delivered),
    };

    public static bool CanTransition(ParcelStatus from, ParcelStatus to) => Allowed.Contains((from, to));
}

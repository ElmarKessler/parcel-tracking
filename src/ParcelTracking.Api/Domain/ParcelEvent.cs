namespace ParcelTracking.Api.Domain;

public class ParcelEvent
{
    public int Id { get; set; }
    public string EventId { get; set; } = string.Empty;
    public int ParcelId { get; set; }
    public ParcelStatus NewStatus { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
}

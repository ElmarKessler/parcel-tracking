namespace ParcelTracking.Api.Domain;

public class Order
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public List<Parcel> Parcels { get; set; } = new();
}

namespace ParcelTracking.Api.Domain;

public class Parcel
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int WarehouseId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public string BinCode { get; set; } = string.Empty;
    public ParcelStatus Status { get; set; } = ParcelStatus.Created;
}

using SaborExpress.Modules.Deliveries.Enum;

namespace SaborExpress.Modules.Deliveries.DTOs
{
    public sealed class CustomerDeliveryTrackingDto
    {
        public int OrderId { get; set; }
        public int DeliveryId { get; set; }
        public DeliveryStatus Status { get; set; }
        public decimal? CourierLatitude { get; set; }
        public decimal? CourierLongitude { get; set; }
        public DateTime? LocationUpdatedAt { get; set; }
    }
}

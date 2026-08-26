// Modules/Deliveries/DTOs/DeliveryResponseDto.cs
using SaborExpress.Modules.Deliveries.Enum;

namespace SaborExpress.Modules.Deliveries.DTOs
{
    public class DeliveryResponseDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int AddressId { get; set; }
        public string? AddressText { get; set; }
        public int DeliveryPersonId { get; set; }
        public string? DeliveryPersonName { get; set; }
        public DeliveryStatus Status { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
    }
}
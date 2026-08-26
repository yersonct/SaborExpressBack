// Modules/Deliveries/DTOs/UpdateDeliveryStatusDto.cs
using SaborExpress.Modules.Deliveries.Enum;

namespace SaborExpress.Modules.Deliveries.DTOs
{
    public class UpdateDeliveryStatusDto
    {
        public DeliveryStatus Status { get; set; }
    }
}
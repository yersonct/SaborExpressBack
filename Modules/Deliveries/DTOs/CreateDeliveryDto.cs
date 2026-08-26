// Modules/Deliveries/DTOs/CreateDeliveryDto.cs
namespace SaborExpress.Modules.Deliveries.DTOs
{
    public class CreateDeliveryDto
    {
        public int OrderId { get; set; }
        public int AddressId { get; set; }
    }
}
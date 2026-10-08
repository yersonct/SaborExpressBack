// Modules/Deliveries/DTOs/CreateDeliveryDto.cs
namespace SaborExpress.Modules.Deliveries.DTOs
{
// CreateDeliveryDto.cs
    public class CreateDeliveryDto
    {
        public int OrderId { get; set; }
        public int DeliveryPersonId { get; set; } // Asignación manual de respaldo — la elige el gerente
    }
}
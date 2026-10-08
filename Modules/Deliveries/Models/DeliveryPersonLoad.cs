// Modules/Deliveries/Models/DeliveryPersonLoad.cs
namespace SaborExpress.Modules.Deliveries.Models
{
    // Proyección liviana: cuántas entregas activas tiene cada repartidor disponible de una sede.
    public class DeliveryPersonLoad
    {
        public int EmployeeId { get; set; }
        public int ActiveDeliveryCount { get; set; }
    }
}
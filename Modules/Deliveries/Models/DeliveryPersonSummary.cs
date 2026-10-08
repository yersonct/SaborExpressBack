namespace SaborExpress.Modules.Deliveries.Models
{
    public class DeliveryPersonSummary
    {
        public int EmployeeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? LastName { get; set; }
        public bool IsAvailable { get; set; }
        public int ActiveDeliveryCount { get; set; }
    }
}
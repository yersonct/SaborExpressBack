namespace SaborExpress.Modules.Configurations.DTOs
{
    public class BranchOperationalSettingsResponseDto
    {
        public int BranchId { get; set; }
        public string OpeningTime { get; set; } = string.Empty; // "HH:mm" o vacío si aún no se configura
        public string ClosingTime { get; set; } = string.Empty;
        public decimal TaxRate { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal DeliveryRadiusKm { get; set; }
        public int MaxActiveDeliveriesPerCourier { get; set; } = 4;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDefault { get; set; } // true = la sede aún no ha guardado nada
    }

    public class UpdateBranchOperationalSettingsDto
    {
        public string OpeningTime { get; set; } = string.Empty; // "HH:mm"
        public string ClosingTime { get; set; } = string.Empty;
        public decimal TaxRate { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal DeliveryRadiusKm { get; set; }
        public int? MaxActiveDeliveriesPerCourier { get; set; }
    }
}

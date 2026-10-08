namespace SaborExpress.Modules.Configurations.DTOs
{
    public class BranchOperationalSettingsResponseDto
    {
        public int BranchId { get; set; }
        public string OpeningTime { get; set; } = "08:00"; // "HH:mm"
        public string ClosingTime { get; set; } = "22:00";
        public decimal TaxRate { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal SuggestedTipPercent { get; set; }
        public decimal DeliveryRadiusKm { get; set; }
        public decimal MinOrderAmount { get; set; }
        public bool AcceptsDelivery { get; set; }
        public bool AcceptsDineIn { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDefault { get; set; } // true = la sede aún no ha guardado nada
    }

    public class UpdateBranchOperationalSettingsDto
    {
        public string OpeningTime { get; set; } = string.Empty; // "HH:mm"
        public string ClosingTime { get; set; } = string.Empty;
        public decimal TaxRate { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal SuggestedTipPercent { get; set; }
        public decimal DeliveryRadiusKm { get; set; }
        public decimal MinOrderAmount { get; set; }
        public bool AcceptsDelivery { get; set; }
        public bool AcceptsDineIn { get; set; }
    }
}
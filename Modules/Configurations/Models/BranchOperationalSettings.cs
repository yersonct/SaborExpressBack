using SaborExpress.Modules.Branches.Models;

namespace SaborExpress.Modules.Configurations.Models
{
    // Una fila por sede. Si no existe, la API devuelve estos mismos valores por defecto.
    public class BranchOperationalSettings
    {
        public int Id { get; set; }

        public int BranchId { get; set; }
        public Branch Branch { get; set; } = null!;

        public TimeOnly OpeningTime { get; set; } = new(8, 0);
        public TimeOnly ClosingTime { get; set; } = new(22, 0);

        public decimal TaxRate { get; set; } = 8m;              
        public decimal DeliveryFee { get; set; }                
        public decimal SuggestedTipPercent { get; set; } = 10m; 
        public decimal DeliveryRadiusKm { get; set; } = 5m;
        public decimal MinOrderAmount { get; set; }             

        public bool AcceptsDelivery { get; set; } = true;
        public bool AcceptsDineIn { get; set; } = true;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
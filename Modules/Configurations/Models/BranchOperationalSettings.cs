using SaborExpress.Modules.Branches.Models;

namespace SaborExpress.Modules.Configurations.Models
{
    // Una fila por sede. Se crea cuando el gerente/administrador guarda por primera vez.
    public class BranchOperationalSettings
    {
        public int Id { get; set; }

        public int BranchId { get; set; }
        public Branch Branch { get; set; } = null!;

        public TimeOnly OpeningTime { get; set; }
        public TimeOnly ClosingTime { get; set; }

        public decimal TaxRate { get; set; } = 8m;
        public decimal DeliveryFee { get; set; }
        public decimal DeliveryRadiusKm { get; set; } = 5m;
        public int MaxActiveDeliveriesPerCourier { get; set; } = 4;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

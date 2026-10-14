using SaborExpress.Modules.Configurations.DTOs;
using SaborExpress.Modules.Configurations.Models;

namespace SaborExpress.Modules.Configurations.Mappings
{
    public static class BranchOperationalSettingsMapper
    {
        public static BranchOperationalSettingsResponseDto ToResponse(BranchOperationalSettings s) => new()
        {
            BranchId = s.BranchId,
            OpeningTime = s.OpeningTime.ToString("HH:mm"),
            ClosingTime = s.ClosingTime.ToString("HH:mm"),
            TaxRate = s.TaxRate,
            DeliveryFee = s.DeliveryFee,
            DeliveryRadiusKm = s.DeliveryRadiusKm,
            MaxActiveDeliveriesPerCourier = s.MaxActiveDeliveriesPerCourier,
            UpdatedAt = s.UpdatedAt,
            IsDefault = false
        };

        // Sede sin configuración guardada: las horas salen vacías para que las llene el gerente/administrador.
        public static BranchOperationalSettingsResponseDto Default(int branchId) => new()
        {
            BranchId = branchId,
            OpeningTime = string.Empty,
            ClosingTime = string.Empty,
            TaxRate = 8m,
            DeliveryFee = 0m,
            DeliveryRadiusKm = 5m,
            MaxActiveDeliveriesPerCourier = 4,
            UpdatedAt = null,
            IsDefault = true
        };
    }
}

using SaborExpress.Modules.Configurations.DTOs;
using SaborExpress.Modules.Configurations.Models;

namespace SaborExpress.Modules.Configurations.Mappings
{
    public static class BranchOperationalSettingsMapper
    {
        public static BranchOperationalSettingsResponseDto ToResponse(BranchOperationalSettings s, bool isDefault = false) => new()
        {
            BranchId = s.BranchId,
            OpeningTime = s.OpeningTime.ToString("HH:mm"),
            ClosingTime = s.ClosingTime.ToString("HH:mm"),
            TaxRate = s.TaxRate,
            DeliveryFee = s.DeliveryFee,
            SuggestedTipPercent = s.SuggestedTipPercent,
            DeliveryRadiusKm = s.DeliveryRadiusKm,
            MinOrderAmount = s.MinOrderAmount,
            AcceptsDelivery = s.AcceptsDelivery,
            AcceptsDineIn = s.AcceptsDineIn,
            UpdatedAt = isDefault ? null : s.UpdatedAt,
            IsDefault = isDefault
        };

        public static BranchOperationalSettingsResponseDto Default(int branchId)
            => ToResponse(new BranchOperationalSettings { BranchId = branchId }, isDefault: true);
    }
}
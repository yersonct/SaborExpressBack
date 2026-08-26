using SaborExpress.Modules.DailyMenu.DTOs;
using SaborExpress.Modules.DailyMenu.Models;

namespace SaborExpress.Modules.DailyMenu.Mappings
{
    public static class DailyMenuMappings
    {
        public static DailyMenuItemResponseDto ToResponseDto(this DailyMenuItem item)
        {
            return new DailyMenuItemResponseDto
            {
                Id = item.Id,
                BranchId = item.BranchId,
                BranchName = item.Branch?.Name ?? string.Empty,
                ProductId = item.ProductId,
                ProductName = item.Product?.Name ?? string.Empty,
                CategoryName = item.Product?.Category?.Name ?? string.Empty,
                ProductPrice = item.Product?.Price ?? 0,
                Date = item.Date,
                MealPeriod = item.MealPeriod.ToString(),
                IsAvailable = item.IsAvailable,
                CreatedAt = item.CreatedAt
            };
        }
    }
}

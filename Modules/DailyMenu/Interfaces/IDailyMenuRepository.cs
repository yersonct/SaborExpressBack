using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SaborExpress.Modules.DailyMenu.Enum;
using SaborExpress.Modules.DailyMenu.Models;

namespace SaborExpress.Modules.DailyMenu.Interfaces
{
    public interface IDailyMenuRepository
    {
        Task<List<DailyMenuItem>> GetByBranchAndDateAsync(int branchId, DateTime date, MealPeriod? period);
        Task<DailyMenuItem?> GetByIdAsync(int id);
        Task<bool> ExistsAsync(int branchId, int productId, DateTime date, MealPeriod period);
        Task AddAsync(DailyMenuItem item);
        Task AddRangeAsync(List<DailyMenuItem> items);
        Task RemoveRangeAsync(int branchId, DateTime date, MealPeriod period);
        Task UpdateAsync(DailyMenuItem item);
        Task DeleteAsync(DailyMenuItem item);
    }
}

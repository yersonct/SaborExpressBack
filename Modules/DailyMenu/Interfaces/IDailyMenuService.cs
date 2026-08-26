using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SaborExpress.Modules.DailyMenu.DTOs;

namespace SaborExpress.Modules.DailyMenu.Interfaces
{
    public interface IDailyMenuService
    {
        Task<List<DailyMenuItemResponseDto>> GetByBranchAndDateAsync(int branchId, DateTime date, string? period);
        Task<List<DailyMenuItemResponseDto>> GetTodayAvailableAsync(int branchId, string? period);
        Task<DailyMenuItemResponseDto> GetByIdAsync(int id);
        Task<DailyMenuItemResponseDto> CreateAsync(CreateDailyMenuItemDto dto, int currentUserId);
        Task<List<DailyMenuItemResponseDto>> BulkSetAsync(BulkSetDailyMenuDto dto, int currentUserId);
        Task<DailyMenuItemResponseDto> UpdateAsync(int id, UpdateDailyMenuItemDto dto, int currentUserId);
        Task<DailyMenuItemResponseDto> ToggleAvailabilityAsync(int id, int currentUserId);
        Task DeleteAsync(int id, int currentUserId);
    }
}
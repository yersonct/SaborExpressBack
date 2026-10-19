// Modules/Tables/Interfaces/ITableService.cs
using SaborExpress.Modules.Tables.DTOs;

namespace SaborExpress.Modules.Tables.Interfaces
{
    public interface ITableService
    {
        Task<List<TableResponseDto>> GetByBranchIdAsync(int branchId, int currentUserId);
        Task<TableResponseDto> GetByIdAsync(int id, int currentUserId);
        Task<TableResponseDto> CreateAsync(CreateTableDto dto, int currentUserId);
        Task<TableResponseDto> UpdateAsync(int id, UpdateTableDto dto, int currentUserId);
        Task<TableResponseDto> UpdateStatusAsync(int id, UpdateTableStatusDto dto, int currentUserId);
        Task DeleteAsync(int id, int currentUserId);
        Task<TableReleaseReviewDto> GetReleaseReviewAsync(int id, int currentUserId);
        Task<List<TableReleaseReviewDto>> GetPreviousDayReleaseReviewsAsync(int branchId, int currentUserId);
        Task<TableResponseDto> ReleasePreviousDayAsync(int id, ReleasePreviousTableDto dto, int currentUserId);
    }
}   
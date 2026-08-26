// Modules/Tables/Interfaces/ITableService.cs
using SaborExpress.Modules.Tables.DTOs;

namespace SaborExpress.Modules.Tables.Interfaces
{
    public interface ITableService
    {
        Task<List<TableResponseDto>> GetByBranchIdAsync(int branchId);
        Task<TableResponseDto> GetByIdAsync(int id);
        Task<TableResponseDto> CreateAsync(CreateTableDto dto, int currentUserId);
        Task<TableResponseDto> UpdateAsync(int id, UpdateTableDto dto, int currentUserId);
        Task<TableResponseDto> UpdateStatusAsync(int id, UpdateTableStatusDto dto, int currentUserId);
        Task DeleteAsync(int id, int currentUserId);
    }
}   
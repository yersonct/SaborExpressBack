using SaborExpress.Modules.Branches.DTOs;

namespace SaborExpress.Modules.Branches.Interfaces
{
public interface IBranchService
    {
        Task<BranchResponseDto> CreateAsync(CreateBranchDto dto);
        Task<BranchResponseDto> UpdateAsync(int id, UpdateBranchDto dto, int currentUserId);
        Task DeleteAsync(int id);
        Task<BranchResponseDto> GetByIdAsync(int id);
        Task<List<BranchResponseDto>> GetAllAsync();
        Task<BranchResponseDto> GetMineAsync(int currentUserId);
    }
}
using SaborExpress.Modules.Branches.DTOs;
using SaborExpress.Modules.Branches.Models;

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
         Task<List<PublicBranchDto>> GetPublicActiveAsync();
         Task<Branch> FindNearestBranchAsync(decimal latitude, decimal longitude);
    }
}
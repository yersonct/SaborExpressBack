using SaborExpress.Modules.Permissions.DTOs;

namespace SaborExpress.Modules.Permissions.Interfaces
{
    public interface IPermissionService
    {
        Task<List<PermissionResponseDto>> GetAllAsync();
        Task<PermissionResponseDto> GetByIdAsync(int id);
        Task<PermissionResponseDto> CreateAsync(CreatePermissionDto dto);
        Task<PermissionResponseDto> UpdateAsync(int id, UpdatePermissionDto dto);
        Task DeleteAsync(int id);
    }
}
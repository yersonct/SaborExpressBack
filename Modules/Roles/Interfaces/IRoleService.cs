using SaborExpress.Modules.Roles.DTOs;

namespace SaborExpress.Modules.Roles.Interfaces
{
    public interface IRoleService
    {
        Task<List<RoleResponseDto>> GetAllAsync();
        Task<RoleResponseDto> GetByIdAsync(int id);
        Task<RoleResponseDto> CreateAsync(CreateRoleDto dto);
        Task<RoleResponseDto> UpdateAsync(int id, UpdateRoleDto dto);
        Task DeleteAsync(int id);
    }
}
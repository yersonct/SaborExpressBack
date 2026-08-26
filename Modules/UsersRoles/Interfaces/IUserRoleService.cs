using SaborExpress.Modules.UserRoles.DTOs;

namespace SaborExpress.Modules.UserRoles.Interfaces
{
    public interface IUserRoleService
    {
        Task<UserRoleResponseDto> AssignAsync(CreateUserRoleDto dto, int currentUserId);
        Task RemoveAsync(int userId, int roleId, int currentUserId);
        Task<List<UserRoleResponseDto>> GetAllAsync();
        Task<List<UserRoleResponseDto>> GetByUserIdAsync(int userId);
    }
}
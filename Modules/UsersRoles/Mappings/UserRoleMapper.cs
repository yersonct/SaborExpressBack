using SaborExpress.Modules.Roles.Models;
using SaborExpress.Modules.UserRoles.DTOs;
using SaborExpress.Modules.UsersRoles.Models;

namespace SaborExpress.Modules.UserRoles.Mappings
{
    public static class UserRoleMapper
    {
        public static UserRoleResponseDto ToResponse(UserRole ur)
        {
            return new UserRoleResponseDto
            {
                UserId = ur.UserId,
                UserEmail = ur.User.Email ?? string.Empty,
                RoleId = ur.RoleId,
                RoleName = ur.Role.Name,
                AssignedAt = ur.AssignedAt
            };
        }
    }
}
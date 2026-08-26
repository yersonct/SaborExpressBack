using SaborExpress.Modules.RolePermissions.DTOs;
using SaborExpress.Modules.RolePermissions.Models;

namespace SaborExpress.Modules.RolePermissions.Mappings
{
    public static class RolePermissionMapper
    {
        public static RolePermissionResponseDto ToResponse(RolePermission rp)
        {
            return new RolePermissionResponseDto
            {
                RoleId = rp.RoleId,
                RoleName = rp.Role.Name,
                PermissionId = rp.PermissionId,
                PermissionName = rp.Permission.Name,
                AssignedAt = rp.AssignedAt
            };
        }
    }
}   
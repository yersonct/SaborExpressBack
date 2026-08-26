// SaborExpress.Modules.Roles.DTOs.RoleWithPermissionsDto
using SaborExpress.Modules.Permissions.DTOs;

namespace SaborExpress.Modules.RolePermissions.DTOs
{
    public class RoleWithPermissionsDto
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public List<PermissionResponseDto> Permissions { get; set; } = new();
    }
}
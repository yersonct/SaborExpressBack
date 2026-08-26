using System;
using SaborExpress.Modules.Permissions.Models;
using SaborExpress.Modules.Roles.Models;

namespace SaborExpress.Modules.RolePermissions.Models
{
    public class RolePermission
    {
        public int RoleId { get; set; }
        public Role Role { get; set; } = null!;

        public int PermissionId { get; set; }
        public Permission Permission { get; set; } = null!;

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}
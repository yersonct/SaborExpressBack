
using SaborExpress.Modules.RolePermissions.Models;

namespace SaborExpress.Modules.Permissions.Models
{
    public class Permission
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public PermissionModule Module { get; set; } 
        public string? Description { get; set; }
        public bool Status { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<RolePermission> RolePermissions { get; set; } = new();

    }
}
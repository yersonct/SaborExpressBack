using System;
using System.Collections.Generic;
using SaborExpress.Modules.Permissions.Models;
using SaborExpress.Modules.RolePermissions.Models;
using SaborExpress.Modules.UsersRoles.Models;

namespace SaborExpress.Modules.Roles.Models
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool RequiresCv { get; set; } = false;
        public bool Status { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<UserRole> UserRoles { get; set; } = new();

        public List<RolePermission> RolePermission { get; set; } = new();
    }
}
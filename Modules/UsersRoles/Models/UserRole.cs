using System;
using SaborExpress.Modules.Auth.Models;
using SaborExpress.Modules.Roles.Models;

namespace SaborExpress.Modules.UsersRoles.Models
{
    public class UserRole
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int RoleId { get; set; }
        public Role Role { get; set; } = null!;

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaborExpress.Modules.Auth.Models
{
    public class User
    {
        public int Id { get; set; }
        public string? Email { get; set; }
        public string? Document { get; set; }
        public string PasswordHash { get; set; }
        public int RoleId { get; set; }
        public string? Token { get; set; }

        public bool Status { get; set; }

        public DateTime? LastLogin { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

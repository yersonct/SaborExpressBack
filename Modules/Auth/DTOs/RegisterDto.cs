using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaborExpress.Modules.Auth.DTOs
{
    public class RegisterDto
    {
        public string? Email { get; set; }

        public string? Document { get; set; }

        public string Password { get; set; } = string.Empty;
        public int RoleId { get; set; }
    
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaborExpress.Modules.Auth.DTOs
{
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string Identifier { get; set; } = string.Empty;

    }
}

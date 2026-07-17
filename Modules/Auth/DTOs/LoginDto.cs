using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaborExpress.Modules.Auth.DTOs
{
    public class LoginDto
    {
        public string Identifier { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
       }
}

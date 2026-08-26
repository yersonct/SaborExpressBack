using System;
using System.Collections.Generic;

namespace SaborExpress.Modules.Auth.DTOs
{
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public string RefreshToken { get; set; } = string.Empty;
        public string Identifier { get; set; } = string.Empty;
    }
}

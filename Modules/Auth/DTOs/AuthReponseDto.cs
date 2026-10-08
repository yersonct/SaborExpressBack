using System;
using System.Collections.Generic;

namespace SaborExpress.Modules.Auth.DTOs
{
    public class AuthResponseDto
    {
        public int UserId { get; set; }
        public string Token { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public string RefreshToken { get; set; } = string.Empty;
        public string Identifier { get; set; } = string.Empty;

        public bool HasActiveShift { get; set; } = true;

        // NUEVO: cuál rol específico tiene el turno activo ahora mismo.
        // Null si no aplica (Cliente/Gerente/Administrador) o si no hay turno activo.
        public string? ActiveShiftRoleName { get; set; }
    }
}
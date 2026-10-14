namespace SaborExpress.Modules.Auth.DTOs
{
    public class ChangePasswordDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        // Opcional: la app móvil actual no lo envía.
        public string? ConfirmPassword { get; set; }
    }
}

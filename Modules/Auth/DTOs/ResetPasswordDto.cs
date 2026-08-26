namespace SaborExpress.Modules.Auth.DTOs
{
    public class ResetPasswordDto
    {
        public string ResetToken { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

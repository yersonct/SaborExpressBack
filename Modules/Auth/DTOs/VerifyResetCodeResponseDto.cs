namespace SaborExpress.Modules.Auth.DTOs
{
    public class VerifyResetCodeResponseDto
    {
        public string ResetToken { get; set; } = string.Empty;
        public int ExpiresInMinutes { get; set; }
    }
}

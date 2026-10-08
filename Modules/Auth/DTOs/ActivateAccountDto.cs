// Modules/Auth/DTOs/ActivateAccountDto.cs
namespace SaborExpress.Modules.Auth.DTOs
{
    public class ActivateAccountDto
    {
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}

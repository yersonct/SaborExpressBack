// Modules/Auth/DTOs/ActivateAccountDto.cs
namespace SaborExpress.Modules.Auth.DTOs
{
    public class ActivateAccountDto
    {
        public string Identifier { get; set; } = string.Empty; // email o documento
        public string Code { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}

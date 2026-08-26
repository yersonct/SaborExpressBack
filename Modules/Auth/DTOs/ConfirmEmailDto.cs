namespace SaborExpress.Modules.Auth.DTOs
{
    public class ConfirmEmailDto
    {
        public string Identifier { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}

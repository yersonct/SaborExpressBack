namespace SaborExpress.Modules.Auth.Models
{
    public interface IVerificationCode
    {
        string Code { get; set; }
        DateTime CodeExpiresAt { get; set; }
        int Attempts { get; set; }
        bool IsUsed { get; set; }
    }
}
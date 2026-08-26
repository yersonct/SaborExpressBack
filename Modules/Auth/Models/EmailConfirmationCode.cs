using System;

namespace SaborExpress.Modules.Auth.Models
{
    public class EmailConfirmationCode : IVerificationCode
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public string Code { get; set; } = string.Empty;
        public DateTime CodeExpiresAt { get; set; }
        public bool IsUsed { get; set; } = false;
        public int Attempts { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
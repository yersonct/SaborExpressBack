using System;

namespace SaborExpress.Modules.Auth.Models
{
    public class PasswordResetCode : IVerificationCode
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public string Code { get; set; } = string.Empty;
        public DateTime CodeExpiresAt { get; set; }
        public bool IsUsed { get; set; }
        public int Attempts { get; set; } = 0;
        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiresAt { get; set; }
        public bool IsCompleted { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
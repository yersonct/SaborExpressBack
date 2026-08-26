using SaborExpress.Modules.Customers.Models;
using SaborExpress.Modules.Employees.Models;
using SaborExpress.Modules.UsersRoles.Models;

namespace SaborExpress.Modules.Auth.Models
{
    public class User
    {
        public int Id { get; set; }
        public string? Email { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public string? Token { get; set; }
        public bool Status { get; set; }

        public bool MustChangePassword { get; set; } = false;
        public DateTime? LastLogin { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockoutEnd { get; set; }

        public List<UserRole> UserRoles { get; set; } = new();
        public Employee? Employee { get; set; }
        public Customer? Customer { get; set; }
        public List<EmployeeActivationCode> EmployeeActivationCodes { get; set; } = new();

        public bool EmailConfirmed { get; set; } = false;
        public List<PasswordResetCode> PasswordResetCodes { get; set; } = new();
    }
}

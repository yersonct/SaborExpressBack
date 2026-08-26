using SaborExpress.Modules.Auth.Models;
using SaborExpress.Modules.Branches.Models;
using System;

namespace SaborExpress.Modules.Employees.Models
{
    public class Employee
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public string Name { get; set; } = string.Empty;
        public string? LastName { get; set; }
        public string Document { get; set; } = string.Empty;

        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Photo { get; set; }
        public int? BranchId { get; set; }
        public Branch? Branch { get; set; }
        public string Status { get; set; } = "Activo";

        public decimal BasePay { get; set; } = 0;

        public byte[]? CvFile { get; set; }
        public string? CvFilename { get; set; }
        public string? CvContentType { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
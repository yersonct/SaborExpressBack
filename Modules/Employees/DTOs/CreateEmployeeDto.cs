using Microsoft.AspNetCore.Http;

namespace SaborExpress.Modules.Employees.DTOs
{
    public class CreateEmployeeDto
    {
        public string Name { get; set; } = string.Empty;
        public string? LastName { get; set; }
        public string Document { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public List<int> RoleIds { get; set; } = new();
        public int? BranchId { get; set; }
        public decimal BasePay { get; set; }

        public IFormFile? Cv { get; set; }
    }
}
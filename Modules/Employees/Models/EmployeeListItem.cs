namespace SaborExpress.Modules.Employees.Models
{
    // Proyeccion liviana para listados: NUNCA trae CvFile (puede pesar varios MB por empleado)
    public class EmployeeListItem
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? LastName { get; set; }
        public string Document { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Photo { get; set; }
        public List<string> RoleNames { get; set; } = new();
        public int? BranchId { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal BasePay { get; set; }
        public bool HasCv { get; set; }
    }
}
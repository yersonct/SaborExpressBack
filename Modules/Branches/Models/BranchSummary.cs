namespace SaborExpress.Modules.Branches.Models
{
    /// <summary>
    /// Proyección liviana para listados: trae el conteo de empleados calculado
    /// directamente en SQL (COUNT), sin cargar la lista completa de Employee a memoria.
    /// </summary>
    public class BranchSummary
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public bool Status { get; set; }
        public int EmployeeCount { get; set; }
    }
}
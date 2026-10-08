namespace SaborExpress.Modules.Employees.DTOs
{
    // Lo único que el propio empleado puede editar de sí mismo.
    public class UpdateEmployeeMeDto
    {
        public string Name { get; set; } = string.Empty;
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public string? Vehicle { get; set; }
        public string? Plate { get; set; }
    }
}
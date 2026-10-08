namespace SaborExpress.Modules.Employees.DTOs
{
    public class EmployeeMeResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public int? BranchId { get; set; }
        public string? BranchName { get; set; }
        public List<string> RoleNames { get; set; } = new();
        public int TablesAttendedToday { get; set; }
        public bool IsAvailable { get; set; }
        public int PaymentsCollectedToday { get; set; }
        public decimal AmountCollectedToday { get; set; }

        // NUEVO — Paso 5
        public string? Vehicle { get; set; }
        public string? Plate { get; set; }

        // NUEVO — Paso 6: solo tiene sentido para Repartidor, queda en 0/null para los demás roles
        public int TripsCompleted { get; set; }
        public double? AverageRating { get; set; }
    }
}
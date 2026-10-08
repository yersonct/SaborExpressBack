namespace SaborExpress.Modules.Branches.DTOs
{
    public class BranchResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public bool Status { get; set; }
        public int EmployeeCount { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
    }
}
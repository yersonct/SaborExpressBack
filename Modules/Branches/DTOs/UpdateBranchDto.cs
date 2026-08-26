namespace SaborExpress.Modules.Branches.DTOs
{
    public class UpdateBranchDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public bool Status { get; set; }
    }
}
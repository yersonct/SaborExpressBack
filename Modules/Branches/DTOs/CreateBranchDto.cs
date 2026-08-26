namespace SaborExpress.Modules.Branches.DTOs
{
    public class CreateBranchDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
    }
}
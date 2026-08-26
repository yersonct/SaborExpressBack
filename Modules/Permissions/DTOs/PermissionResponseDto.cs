namespace SaborExpress.Modules.Permissions.DTOs
{
    public class PermissionResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        
        public string Module { get; set; } = string.Empty;
        public bool Status { get; set; }
        public string? Description { get; set; }

    }
}
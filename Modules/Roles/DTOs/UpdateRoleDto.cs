namespace SaborExpress.Modules.Roles.DTOs
{
    public class UpdateRoleDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool RequiresCv { get; set; }
        public bool Status { get; set; }
    }
}
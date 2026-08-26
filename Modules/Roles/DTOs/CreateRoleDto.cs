namespace SaborExpress.Modules.Roles.DTOs
{
    public class CreateRoleDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool RequiresCv { get; set; } = false;
    }
}
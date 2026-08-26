using SaborExpress.Modules.Permissions.Models;

namespace SaborExpress.Modules.Permissions.DTOs
{
    public class CreatePermissionDto
    {
        public string Name { get; set; } = string.Empty;
        public PermissionModule Module { get; set; }
        public string? Description { get; set; }
    }
}
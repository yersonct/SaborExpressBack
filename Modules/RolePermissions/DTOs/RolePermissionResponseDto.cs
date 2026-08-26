namespace SaborExpress.Modules.RolePermissions.DTOs
{
    public class RolePermissionResponseDto
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public int PermissionId { get; set; }
        public string PermissionName { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
    }
}
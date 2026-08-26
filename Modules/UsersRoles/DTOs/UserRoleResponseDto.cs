namespace SaborExpress.Modules.UserRoles.DTOs
{
    public class UserRoleResponseDto
    {
        public int UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
    }
}
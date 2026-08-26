using SaborExpress.Modules.Permissions.DTOs;
using SaborExpress.Modules.Permissions.Models;

namespace SaborExpress.Modules.Permissions.Mappings
{
    public static class PermissionMapper
    {
        public static PermissionResponseDto ToResponse(Permission permission)
        {
            return new PermissionResponseDto
            {
                Id = permission.Id,
                Name = permission.Name,
                Module = permission.Module.ToString(),
                Status = permission.Status,
                Description = permission.Description
            };
        }
    }
}
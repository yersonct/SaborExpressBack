using SaborExpress.Modules.Roles.DTOs;
using SaborExpress.Modules.Roles.Models;

namespace SaborExpress.Modules.Roles.Mappings
{
    public static class RoleMapper
    {
        public static RoleResponseDto ToResponse(Role role)
        {
            return new RoleResponseDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                RequiresCv = role.RequiresCv,
                Status = role.Status
            };
        }
    }
}
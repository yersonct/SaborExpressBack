using SaborExpress.Modules.Auth.DTOs;
using SaborExpress.Modules.Auth.Models;

namespace SaborExpress.Modules.Auth.Mappings
{
    public static class AuthMapper
    {
        public static AuthResponseDto ToAuthResponse(User user, string token)
        {
            return new AuthResponseDto
            {
                Token = token,
                RoleId = user.RoleId,
                Identifier = user.Email ?? user.Document ?? "Unknown"
            };
        }
    }
}
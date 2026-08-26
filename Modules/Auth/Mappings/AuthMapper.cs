using SaborExpress.Modules.Auth.DTOs;
using SaborExpress.Modules.Auth.Models;

namespace SaborExpress.Modules.Auth.Mappings
{
    public static class AuthMapper
    {
        public static AuthResponseDto ToAuthResponse(User user, string token, string refreshToken)
        {
            return new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
                Identifier = user.Email ?? "Unknown"
            };
        }
    }
}

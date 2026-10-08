using SaborExpress.Modules.Auth.DTOs;
using SaborExpress.Modules.Auth.Models;

namespace SaborExpress.Modules.Auth.Mappings
{
    public static class AuthMapper
    {
        public static AuthResponseDto ToAuthResponse(
            User user, string token, string refreshToken, bool hasActiveShift, string? activeShiftRoleName)
        {
            return new AuthResponseDto
            {
                UserId = user.Id,
                Token = token,
                RefreshToken = refreshToken,
                Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
                Identifier = user.Email ?? "Unknown",
                HasActiveShift = hasActiveShift,
                ActiveShiftRoleName = activeShiftRoleName
            };
        }
    }
}
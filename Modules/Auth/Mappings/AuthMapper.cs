using SaborExpress.Modules.Auth.DTOs;
using SaborExpress.Modules.Auth.Models;

namespace SaborExpress.Modules.Auth.Mappings
{
    public static class AuthMapper
    {
        public static AuthResponseDto ToAuthResponse(
            User user, string token, string refreshToken, bool hasActiveShift, string? activeShiftRoleName, IReadOnlyCollection<string>? effectiveRoles = null)
        {
            return new AuthResponseDto
            {
                UserId = user.Id,
                Token = token,
                RefreshToken = refreshToken,
                Roles = (effectiveRoles ?? user.UserRoles.Select(ur => ur.Role.Name).ToList()).ToList(),
                Identifier = user.Email ?? "Unknown",
                HasActiveShift = hasActiveShift,
                ActiveShiftRoleName = activeShiftRoleName
            };
        }
    }
}

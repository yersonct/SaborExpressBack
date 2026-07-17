using SaborExpress.Modules.Auth.DTOs;
using SaborExpress.Modules.Auth.Models;

namespace SaborExpress.Modules.Auth.Factories
{
    public static class UserFactory
    {
        public static User Create(RegisterDto dto)
        {
            return new User
            {
                Email = string.IsNullOrWhiteSpace(dto.Email)
                    ? null
                    : dto.Email,

                Document = string.IsNullOrWhiteSpace(dto.Document)
                    ? null
                    : dto.Document,

                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),

                RoleId = dto.RoleId,

                Status = true,

                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
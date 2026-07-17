using SaborExpress.Modules.Auth.DTOs;
using SaborExpress.Modules.Auth.Models;

namespace SaborExpress.Modules.Auth.Validators
{
    public class LoginValidator
    {
        public void Validate(User? user, LoginDto loginDto)
        {
            if (user == null ||
                !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Credenciales incorrectas.");
            }

            if (!user.Status)
            {
                throw new UnauthorizedAccessException("La cuenta está deshabilitada.");
            }
        }
    }
}
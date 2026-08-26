using SaborExpress.Modules.Auth.DTOs;
using SaborExpress.Modules.Auth.Models;

namespace SaborExpress.Modules.Auth.Validators
{
    public class LoginValidator
    {
        public void ValidateAccountRules(User user, LoginDto loginDto)
        {
            if (!user.Status)
                throw new UnauthorizedAccessException("La cuenta esta deshabilitada.");

            if (user.Employee != null && user.Employee.Status == "Retirado")
                throw new UnauthorizedAccessException("Esta cuenta de empleado ha sido retirada.");

            if (!user.EmailConfirmed)
                throw new UnauthorizedAccessException("Debes confirmar tu correo antes de iniciar sesion.");
        }
    }
}
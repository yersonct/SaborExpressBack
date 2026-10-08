// Modules/Auth/Validators/ActivateAccountValidator.cs
using SaborExpress.Modules.Auth.DTOs;

namespace SaborExpress.Modules.Auth.Validators
{
    public class ActivateAccountValidator
    {
        public void Validate(ActivateAccountDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Token))
                throw new ArgumentException("El enlace de activacion es invalido");

            if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 8)
                throw new ArgumentException("La nueva contrasena debe tener al menos 8 caracteres");
        }
    }
}

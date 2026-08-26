// Modules/Auth/Validators/ActivateAccountValidator.cs
using SaborExpress.Modules.Auth.DTOs;

namespace SaborExpress.Modules.Auth.Validators
{
    public class ActivateAccountValidator
    {
        public void Validate(ActivateAccountDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Identifier))
                throw new ArgumentException("El identificador es obligatorio");

            if (string.IsNullOrWhiteSpace(dto.Code))
                throw new ArgumentException("El codigo es obligatorio");

            if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 8)
                throw new ArgumentException("La nueva contrasena debe tener al menos 8 caracteres");
        }
    }
}

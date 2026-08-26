using System;
using SaborExpress.Modules.Auth.DTOs;

namespace SaborExpress.Modules.Auth.Validators
{
    public class ResetPasswordValidator
    {
        public void Validate(ResetPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ResetToken))
                throw new ArgumentException("Token de recuperacion invalido.");

            if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 8)
                throw new ArgumentException("La contrasena debe tener al menos 8 caracteres.");

            if (dto.NewPassword != dto.ConfirmPassword)
                throw new ArgumentException("Las contrasenas no coinciden.");
        }
    }
}

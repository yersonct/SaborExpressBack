using System;
using System.Threading.Tasks;
using SaborExpress.Modules.Auth.DTOs;

namespace SaborExpress.Modules.Auth.Validators
{
    public class ForgotPasswordValidator
    {
        public Task ValidateAsync(ForgotPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Identifier))
                throw new ArgumentException("Debes ingresar tu correo o documento.");

            if (dto.Identifier.Length > 100)
                throw new ArgumentException("El campo ingresado es demasiado largo.");

            return Task.CompletedTask;
        }
    }
}

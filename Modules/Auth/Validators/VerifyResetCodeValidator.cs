using System;
using System.Linq;
using SaborExpress.Modules.Auth.DTOs;

namespace SaborExpress.Modules.Auth.Validators
{
    public class VerifyResetCodeValidator
    {
        public void Validate(VerifyResetCodeDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Identifier))
                throw new ArgumentException("Debes ingresar tu correo o documento.");

            if (string.IsNullOrWhiteSpace(dto.Code) || dto.Code.Length != 6 || !dto.Code.All(char.IsDigit))
                throw new ArgumentException("El codigo debe tener 6 digitos.");
        }
    }
}

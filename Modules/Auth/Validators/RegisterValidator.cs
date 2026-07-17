using SaborExpress.Modules.Auth.DTOs;
using SaborExpress.Modules.Auth.Interfaces;

namespace SaborExpress.Modules.Auth.Validators
{
    public class RegisterValidator
    {
        private readonly IAuthRepository _authRepository;

        public RegisterValidator(IAuthRepository authRepository)
        {
            _authRepository = authRepository;
        }

        public async Task ValidateAsync(RegisterDto registerDto)
        {
            if (string.IsNullOrWhiteSpace(registerDto.Email) &&
                string.IsNullOrWhiteSpace(registerDto.Document))
            {
                throw new ArgumentException("Debe proporcionar un correo o documento.");
            }

            if (!string.IsNullOrWhiteSpace(registerDto.Email) &&
                await _authRepository.ExistsByEmailAsync(registerDto.Email))
            {
                throw new Exception("El correo ya existe.");
            }

            if (!string.IsNullOrWhiteSpace(registerDto.Document) &&
                await _authRepository.ExistsByDocumentAsync(registerDto.Document))
            {
                throw new Exception("El documento ya existe.");
            }
        }
    }
}
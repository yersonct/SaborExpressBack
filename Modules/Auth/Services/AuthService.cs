using SaborExpress.Modules.Auth.DTOs;
using SaborExpress.Modules.Auth.Factories;
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Auth.Mappings;
using SaborExpress.Modules.Auth.Validators;
using SaborExpress.Modules.Auth.Helpers;
using System;
using System.Threading.Tasks;

namespace SaborExpress.Modules.Auth.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly RegisterValidator _registerValidator;
        private readonly LoginValidator _loginValidator;
        private readonly JwtTokenGenerator _jwtGenerator;

        public AuthService(
            IAuthRepository authRepository,
            RegisterValidator registerValidator,
            LoginValidator loginValidator,
            JwtTokenGenerator jwtGenerator)
        {
            _authRepository = authRepository;
            _registerValidator = registerValidator;
            _loginValidator = loginValidator;
            _jwtGenerator = jwtGenerator;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            // 1. Validar reglas de negocio
            await _registerValidator.ValidateAsync(dto);

            // 2. Crear la entidad (Aquí se debería aplicar el BCrypt al Password)
            var user = UserFactory.Create(dto);

            // 3. Guardar en BD
            await _authRepository.AddAsync(user);

            // 4. Generar Token
            var token = _jwtGenerator.Generate(
                user,
                user.Email ?? user.Document ?? "Unknown");

            // 5. Mapear a respuesta
            return AuthMapper.ToAuthResponse(user, token);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            // 1. Buscar usuario
            var user = await _authRepository.GetByIdentifierAsync(dto.Identifier);

            // 2. Validar que exista, esté activo y la contraseña (BCrypt) coincida
            // Nota: El validador debe lanzar una excepción si falla.
            _loginValidator.Validate(user, dto);

            // 3. Actualizar último login
            user!.LastLogin = DateTime.UtcNow;
            await _authRepository.UpdateAsync(user);

            // 4. Generar Token
            var token = _jwtGenerator.Generate(user, dto.Identifier);

            // 5. Mapear a respuesta
            return AuthMapper.ToAuthResponse(user, token);
        }
    }
}
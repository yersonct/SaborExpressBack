using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Customers.DTOs;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SaborExpress.Modules.Customers.Validators
{
    public class CustomerValidator
    {
        private const int MinPasswordLength = 8;
        private const int MinAddressLength = 5;
        private const int MaxAddressLength = 150;

        private static readonly Regex PhoneRegex = new(@"^\d{7}(\d{3})?$", RegexOptions.Compiled);

        private readonly IAuthRepository _authRepository;

        public CustomerValidator(IAuthRepository authRepository)
            => _authRepository = authRepository;

        public async Task ValidateAsync(RegisterCustomerDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Any(char.IsDigit))
                throw new ArgumentException("El nombre no puede estar vacío ni contener números.");

            if (string.IsNullOrWhiteSpace(dto.LastName) || dto.LastName.Any(char.IsDigit))
                throw new ArgumentException("El apellido no puede estar vacío ni contener números.");

            if (string.IsNullOrWhiteSpace(dto.Email) ||
                !Regex.IsMatch(dto.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                throw new ArgumentException("El correo electrónico no tiene un formato válido.");

            if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < MinPasswordLength)
                throw new ArgumentException($"La contraseña debe tener al menos {MinPasswordLength} caracteres.");

            if (!string.IsNullOrWhiteSpace(dto.Phone) && !PhoneRegex.IsMatch(dto.Phone))
                throw new ArgumentException("El teléfono debe tener 7 dígitos (fijo) o 10 dígitos (celular), sin espacios ni guiones.");

            if (!string.IsNullOrWhiteSpace(dto.Address) &&
                (dto.Address.Length < MinAddressLength || dto.Address.Length > MaxAddressLength))
                throw new ArgumentException($"La dirección debe tener entre {MinAddressLength} y {MaxAddressLength} caracteres.");

            if (await _authRepository.ExistsByEmailAsync(dto.Email))
                throw new ArgumentException("El correo ya está registrado.");   // 👈 antes era "throw new Exception(...)"
        }

        public void ValidateUpdate(UpdateCustomerDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Any(char.IsDigit))
                throw new ArgumentException("El nombre no puede estar vacío ni contener números.");

            if (!string.IsNullOrWhiteSpace(dto.LastName) && dto.LastName.Any(char.IsDigit))
                throw new ArgumentException("El apellido no puede contener números.");

            if (!string.IsNullOrWhiteSpace(dto.Phone) && !PhoneRegex.IsMatch(dto.Phone))
                throw new ArgumentException("El teléfono debe tener 7 dígitos (fijo) o 10 dígitos (celular), sin espacios ni guiones.");

            if (!string.IsNullOrWhiteSpace(dto.Address) &&
                (dto.Address.Length < MinAddressLength || dto.Address.Length > MaxAddressLength))
                throw new ArgumentException($"La dirección debe tener entre {MinAddressLength} y {MaxAddressLength} caracteres.");
        }
    }
}
    using SaborExpress.Modules.Branches.DTOs;
    using SaborExpress.Modules.Branches.Interfaces;
    using System;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    namespace SaborExpress.Modules.Branches.Validators
    {
        public class BranchValidator
        {
            private const int MaxNameLength = 50;
            private const int MaxAddressLength = 150;
            private const int MinAddressLength = 5;

            private static readonly Regex PhoneRegex = new(@"^\d{7}(\d{3})?$", RegexOptions.Compiled);

            private readonly IBranchRepository _branchRepository;

            public BranchValidator(IBranchRepository branchRepository)
                => _branchRepository = branchRepository;

            public async Task ValidateCreateAsync(CreateBranchDto dto)
            {
                ValidateName(dto.Name);
                ValidateAddress(dto.Address);
                ValidatePhone(dto.Phone);

                if (await _branchRepository.ExistsByNameAsync(dto.Name))
                    throw new ArgumentException($"La sede '{dto.Name}' ya se encuentra registrada.");
            }

            public async Task ValidateUpdateAsync(int id, UpdateBranchDto dto)
            {
                ValidateName(dto.Name);
                ValidateAddress(dto.Address);
                ValidatePhone(dto.Phone);

                if (await _branchRepository.ExistsByNameAsync(dto.Name, id))
                    throw new ArgumentException($"La sede '{dto.Name}' ya se encuentra registrada.");
            }

            

            private static void ValidateName(string name)
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("El nombre de la sede es obligatorio.");

                if (name.Length > MaxNameLength)
                    throw new ArgumentException($"El nombre de la sede no puede superar los {MaxNameLength} caracteres.");
            }

            private static void ValidateAddress(string? address)
            {
                if (string.IsNullOrWhiteSpace(address))
                    throw new ArgumentException("La dirección de la sede es obligatoria.");

                if (address.Length < MinAddressLength)
                    throw new ArgumentException($"La dirección debe tener al menos {MinAddressLength} caracteres.");

                if (address.Length > MaxAddressLength)
                    throw new ArgumentException($"La dirección no puede superar los {MaxAddressLength} caracteres.");
            }

            private static void ValidatePhone(string? phone)
            {
                if (string.IsNullOrWhiteSpace(phone))
                    throw new ArgumentException("El teléfono de la sede es obligatorio.");

                if (!PhoneRegex.IsMatch(phone))
                    throw new ArgumentException("El teléfono debe tener 7 dígitos (fijo) o 10 dígitos (celular), sin espacios ni guiones.");
            }
        }
    }
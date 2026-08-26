using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Auth.Models;
using SaborExpress.Modules.Customers.DTOs;
using SaborExpress.Modules.Customers.Interfaces;
using SaborExpress.Modules.Customers.Mappings;   
using SaborExpress.Modules.Customers.Models;
using SaborExpress.Modules.Customers.Validators;
using SaborExpress.Modules.Roles.Interfaces;
using SaborExpress.Modules.UsersRoles.Models;
using SaborExpress.Shared.Constants;

namespace SaborExpress.Modules.Customers.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly CustomerValidator _validator;
        private readonly IEmailConfirmationService _emailConfirmationService;
        private readonly IRoleRepository _roleRepository;

        public CustomerService(
            ICustomerRepository customerRepository,
            CustomerValidator validator,
            IEmailConfirmationService emailConfirmationService,
            IRoleRepository roleRepository)
        {
            _customerRepository = customerRepository;
            _validator = validator;
            _emailConfirmationService = emailConfirmationService;
            _roleRepository = roleRepository;
        }

        public async Task<RegisterResponseDto> RegisterAsync(RegisterCustomerDto dto)
        {
            await _validator.ValidateAsync(dto);

            var clienteRole = await _roleRepository.GetByNameAsync(RoleNames.Cliente)
                ?? throw new InvalidOperationException("El rol CLIENTE no está configurado en el sistema.");

            var customer = new Customer
            {
                Name = dto.Name,
                LastName = dto.LastName,
                Phone = dto.Phone,
                Address = dto.Address,
                User = new User
                {
                    Email = dto.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    Status = true,
                    EmailConfirmed = false,
                    UserRoles = new List<UserRole>
                    {
                        new UserRole { Role = clienteRole }
                    }
                }
            };

            await _customerRepository.AddAsync(customer);

            await _emailConfirmationService.SendConfirmationCodeAsync(customer.User);

            return new RegisterResponseDto
            {
                Message = "Registro exitoso. Revisa tu correo para confirmar tu cuenta antes de iniciar sesión."
            };
        }

        public async Task<CustomerResponseDto> GetMeAsync(int userId)
        {
            var customer = await _customerRepository.GetByUserIdAsync(userId)
                ?? throw new KeyNotFoundException("No se encontró información de cliente para este usuario.");

            return CustomerMapper.ToResponse(customer);
        }

        public async Task<CustomerResponseDto> UpdateMeAsync(int userId, UpdateCustomerDto dto)
        {
            _validator.ValidateUpdate(dto);

            var customer = await _customerRepository.GetByUserIdAsync(userId)
                ?? throw new KeyNotFoundException("No se encontró información de cliente para este usuario.");

            customer.Name = dto.Name;
            customer.LastName = dto.LastName;
            customer.Phone = dto.Phone;
            customer.Address = dto.Address;

            await _customerRepository.UpdateAsync(customer);
            return CustomerMapper.ToResponse(customer);
        }

        public async Task<List<CustomerResponseDto>> GetAllAsync()
        {
            var customers = await _customerRepository.GetAllAsync();
            return customers.Select(CustomerMapper.ToResponse).ToList();
        }

        public async Task<CustomerResponseDto> GetByIdAsync(int id)
        {
            var customer = await _customerRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("El cliente no existe.");

            return CustomerMapper.ToResponse(customer);
        }
    }
}
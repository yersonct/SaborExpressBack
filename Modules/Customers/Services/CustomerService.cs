using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Auth.Models;
using SaborExpress.Modules.Customers.DTOs;
using SaborExpress.Modules.Customers.Interfaces;
using SaborExpress.Modules.Customers.Mappings;   
using SaborExpress.Modules.Customers.Models;
using SaborExpress.Modules.Customers.Validators;
using SaborExpress.Modules.Roles.Interfaces;
using SaborExpress.Modules.UsersRoles.Models;
using SaborExpress.Modules.Employees.Interfaces;
using SaborExpress.Shared.Constants;

namespace SaborExpress.Modules.Customers.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly CustomerValidator _validator;
        private readonly IEmailConfirmationService _emailConfirmationService;
        private readonly IRoleRepository _roleRepository;
        private readonly IEmployeeRepository _employeeRepository;

        public CustomerService(
            ICustomerRepository customerRepository,
            CustomerValidator validator,
            IEmailConfirmationService emailConfirmationService,
            IRoleRepository roleRepository,
            IEmployeeRepository employeeRepository)
        {
            _customerRepository = customerRepository;
            _validator = validator;
            _emailConfirmationService = emailConfirmationService;
            _roleRepository = roleRepository;
            _employeeRepository = employeeRepository;
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
                Document = dto.Document,
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
            var customer = await _customerRepository.GetByUserIdAsync(userId);
            var email = await _customerRepository.GetEmailByUserIdAsync(userId);

            if (customer != null)
                return CustomerMapper.ToResponse(customer, email);

            // No es cliente registrado: es un empleado sin turno activo usando
            // la app como cliente. No se crea ninguna fila nueva, se "presta"
            // su propio dato de Employee para esta vista.
            var employee = await _employeeRepository.GetByUserIdAsync(userId)
                ?? throw new KeyNotFoundException("No se encontró información de perfil para este usuario.");

            return new CustomerResponseDto
            {
                Id = 0, // no aplica: no hay fila de Customer real
                UserId = userId,
                Name = employee.Name,
                LastName = employee.LastName,
                Document = employee.Document,
                Phone = employee.Phone,
                Address = employee.Address,
                Email = email
            };
        }

        public async Task<CustomerResponseDto> UpdateMeAsync(int userId, UpdateCustomerDto dto)
        {
            _validator.ValidateUpdate(dto);

            var customer = await _customerRepository.GetByUserIdAsync(userId);

            if (customer != null)
            {
                customer.Name = dto.Name;
                customer.LastName = dto.LastName;
                customer.Phone = dto.Phone;
                customer.Address = dto.Address;

                await _customerRepository.UpdateAsync(customer);

                var email = await _customerRepository.GetEmailByUserIdAsync(userId);
                return CustomerMapper.ToResponse(customer, email);
            }

            // Mismo caso: empleado editando "su perfil de cliente" — el cambio
            // se guarda en su propio registro de Employee, no se crea un Customer.
            var employee = await _employeeRepository.GetByUserIdAsync(userId)
                ?? throw new KeyNotFoundException("No se encontró información de perfil para este usuario.");

            employee.Name = dto.Name;
            employee.LastName = dto.LastName;
            employee.Phone = dto.Phone;
            employee.Address = dto.Address;

            await _employeeRepository.UpdateAsync(employee);

            var emailEmployee = await _customerRepository.GetEmailByUserIdAsync(userId);
            return new CustomerResponseDto
            {
                Id = 0,
                UserId = userId,
                Name = employee.Name,
                LastName = employee.LastName,
                Document = employee.Document,
                Phone = employee.Phone,
                Address = employee.Address,
                Email = emailEmployee
            };
        }

        public async Task<List<CustomerResponseDto>> GetAllAsync()
        {
            var customers = await _customerRepository.GetAllAsync();
            return customers.Select(c => CustomerMapper.ToResponse(c)).ToList();
        }

        public async Task<CustomerResponseDto> GetByIdAsync(int id)
        {
            var customer = await _customerRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("El cliente no existe.");

            return CustomerMapper.ToResponse(customer);
        }
    }
}
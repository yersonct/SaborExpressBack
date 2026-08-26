using SaborExpress.Modules.Auth.DTOs;
using SaborExpress.Modules.Customers.DTOs;

namespace SaborExpress.Modules.Customers.Interfaces
{
    public interface ICustomerService
    {
        Task<RegisterResponseDto> RegisterAsync(RegisterCustomerDto dto);
        Task<CustomerResponseDto> GetMeAsync(int userId);
        Task<CustomerResponseDto> UpdateMeAsync(int userId, UpdateCustomerDto dto);
        Task<List<CustomerResponseDto>> GetAllAsync();
        Task<CustomerResponseDto> GetByIdAsync(int id);
        }
}
// Modules/Addresses/Interfaces/IAddressService.cs
using SaborExpress.Modules.Addresses.DTOs;
using SaborExpress.Modules.Addresses.Models;

namespace SaborExpress.Modules.Addresses.Interfaces
{
    public interface IAddressService
    {
        Task<List<AddressResponseDto>> GetByCustomerIdAsync(int customerId, int currentUserId);
        Task<AddressResponseDto> GetByIdAsync(int id, int currentUserId);
        Task<AddressResponseDto> CreateAsync(CreateAddressDto dto, int customerId);
        Task<AddressResponseDto> UpdateAsync(int id, UpdateAddressDto dto, int customerId);
        Task<AddressResponseDto> SetDefaultAsync(int id, int customerId);
        Task DeleteAsync(int id, int customerId);
        Task<AddressResponseDto?> GetDefaultForCustomerAsync(int customerId);


    }
}
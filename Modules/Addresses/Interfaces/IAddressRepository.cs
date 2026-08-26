// Modules/Addresses/Interfaces/IAddressRepository.cs
using SaborExpress.Modules.Addresses.Models;

namespace SaborExpress.Modules.Addresses.Interfaces
{
    public interface IAddressRepository
    {
        Task<Address?> GetByIdAsync(int id);
        Task<List<Address>> GetByCustomerIdAsync(int customerId);
        Task<bool> CustomerExistsAsync(int customerId);
        Task ClearDefaultForCustomerAsync(int customerId, int? excludeAddressId = null);

        Task AddAsync(Address address);
        Task UpdateAsync(Address address);
        Task DeleteAsync(Address address);
        Task SaveChangesAsync();
    }
}
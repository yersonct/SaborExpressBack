// Modules/Addresses/Repositories/AddressRepository.cs
using Microsoft.EntityFrameworkCore;
using SaborExpress.Data;
using SaborExpress.Modules.Addresses.Interfaces;
using SaborExpress.Modules.Addresses.Models;

namespace SaborExpress.Modules.Addresses.Repositories
{
    public class AddressRepository : IAddressRepository
    {
        private readonly AppDbContext _context;

        public AddressRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Address?> GetByIdAsync(int id)
        {
            return await _context.Addresses.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<Address>> GetByCustomerIdAsync(int customerId)
        {
            return await _context.Addresses
                .Where(x => x.CustomerId == customerId)
                .OrderByDescending(x => x.IsDefault)
                .ToListAsync();
        }

        public async Task<bool> CustomerExistsAsync(int customerId)
        {
            return await _context.Customers.AnyAsync(x => x.Id == customerId);
        }

        public async Task ClearDefaultForCustomerAsync(int customerId, int? excludeAddressId = null)
        {
            var addresses = await _context.Addresses
                .Where(x => x.CustomerId == customerId &&
                            x.IsDefault &&
                            (!excludeAddressId.HasValue || x.Id != excludeAddressId.Value))
                .ToListAsync();

            foreach (var address in addresses)
                address.IsDefault = false;
        }

        public async Task AddAsync(Address address)
        {
            await _context.Addresses.AddAsync(address);
        }

        public Task UpdateAsync(Address address)
        {
            _context.Addresses.Update(address);
            return Task.CompletedTask;
        }
        public async Task<bool> IsAssignedToDeliveryPersonAsync(int addressId, int employeeId)
        {
            return await _context.Deliveries
                .AnyAsync(d => d.AddressId == addressId && d.DeliveryPersonId == employeeId);
        }
        public Task DeleteAsync(Address address)
        {
            _context.Addresses.Remove(address);
            return Task.CompletedTask;
        }
        public async Task<Address?> GetDefaultByCustomerIdAsync(int customerId)
        {
            return await _context.Addresses
                .FirstOrDefaultAsync(x => x.CustomerId == customerId && x.IsDefault);
        }

        public async Task<Address?> GetMostRecentByCustomerIdAsync(int customerId, int? excludeAddressId = null)
        {
            return await _context.Addresses
                .Where(x => x.CustomerId == customerId &&
                            (!excludeAddressId.HasValue || x.Id != excludeAddressId.Value))
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();
        }

        // Solo modifica la entidad; se guarda con el SaveChangesAsync del servicio,
        // así todo queda en una sola transacción.
        public async Task SetCustomerAddressTextAsync(int customerId, string? addressText)
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == customerId);
            if (customer == null) return;

            // Customers.Address admite máximo 200 caracteres.
            customer.Address = addressText is { Length: > 200 }
                ? addressText[..200]
                : addressText;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
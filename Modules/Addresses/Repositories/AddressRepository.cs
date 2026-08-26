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

        public Task DeleteAsync(Address address)
        {
            _context.Addresses.Remove(address);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
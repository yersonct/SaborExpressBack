using SaborExpress.Data;
using SaborExpress.Modules.Customers.Interfaces;
using Microsoft.EntityFrameworkCore;
using SaborExpress.Modules.Customers.Models;

namespace SaborExpress.Modules.Customers.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly AppDbContext _context;

        public CustomerRepository(AppDbContext context) => _context = context;

        public async Task AddAsync(Customer customer)
        {
            await _context.Customers.AddAsync(customer);
            await _context.SaveChangesAsync();
        }

        // Ya no hace falta Include(c => c.User): el UserId ya vive en Customer,
        // y el mapper nunca usa datos de User.
        public async Task<Customer?> GetByUserIdAsync(int userId)
            => await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);

        public async Task<Customer?> GetByIdAsync(int id)
            => await _context.Customers.FirstOrDefaultAsync(c => c.Id == id);

        public async Task<List<Customer>> GetAllAsync()
            => await _context.Customers.ToListAsync();

        public async Task UpdateAsync(Customer customer)
        {
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();
        }
    }
}
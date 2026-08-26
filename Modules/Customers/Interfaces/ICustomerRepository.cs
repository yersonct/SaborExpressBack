using SaborExpress.Modules.Customers.Models;

namespace SaborExpress.Modules.Customers.Interfaces
{
    public interface ICustomerRepository
    {
        Task AddAsync(Customer customer);
        Task<Customer?> GetByIdAsync(int id);
        Task<Customer?> GetByUserIdAsync(int userId);
        Task<List<Customer>> GetAllAsync();
        Task UpdateAsync(Customer customer);
    }
}
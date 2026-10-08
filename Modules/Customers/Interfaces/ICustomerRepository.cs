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

        // Puente Customer.Id -> User.Id, usado por notificaciones (crear una
        // notificación exige el UserId, pero los pedidos solo tienen CustomerId).
        Task<int?> GetUserIdByCustomerIdAsync(int customerId);
        Task<string?> GetEmailByUserIdAsync(int userId);
    }
}
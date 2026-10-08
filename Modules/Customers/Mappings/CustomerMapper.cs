using SaborExpress.Modules.Customers.DTOs;
using SaborExpress.Modules.Customers.Models;

namespace SaborExpress.Modules.Customers.Mappings
{
    public static class CustomerMapper
    {
        public static CustomerResponseDto ToResponse(Customer customer, string? email = null) => new()
        {
            Id = customer.Id,
            UserId = customer.UserId,
            Name = customer.Name,
            LastName = customer.LastName,
            Document = customer.Document,
            Phone = customer.Phone,
            Address = customer.Address,
            Email = email
        };
    }
}
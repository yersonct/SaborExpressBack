using SaborExpress.Modules.Customers.DTOs;
using SaborExpress.Modules.Customers.Models;

namespace SaborExpress.Modules.Customers.Mappings
{
    public static class CustomerMapper
    {
        public static CustomerResponseDto ToResponse(Customer customer) => new()
        {
            Id = customer.Id,
            UserId = customer.UserId,
            Name = customer.Name,
            LastName = customer.LastName,
            Phone = customer.Phone,
            Address = customer.Address
        };
    }
}
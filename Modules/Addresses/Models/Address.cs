// Modules/Addresses/Models/Address.cs
using SaborExpress.Modules.Customers.Models;

namespace SaborExpress.Modules.Addresses.Models
{
    public class Address
    {
        public int Id { get; set; }

        public int CustomerId { get; set; } // FK
        public Customer Customer { get; set; } = null!;

        public string AddressLine { get; set; } = string.Empty;
        public string? Reference { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public bool IsDefault { get; set; } = false;
    }
}
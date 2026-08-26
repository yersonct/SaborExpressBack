// Modules/Addresses/Mappings/AddressMapper.cs
using SaborExpress.Modules.Addresses.DTOs;
using SaborExpress.Modules.Addresses.Models;

namespace SaborExpress.Modules.Addresses.Mappings
{
    public static class AddressMapper
    {
        public static AddressResponseDto ToResponse(Address address)
        {
            return new AddressResponseDto
            {
                Id = address.Id,
                CustomerId = address.CustomerId,
                AddressLine = address.AddressLine,
                Reference = address.Reference,
                Latitude = address.Latitude,
                Longitude = address.Longitude,
                IsDefault = address.IsDefault
            };
        }
    }
}
// Modules/Addresses/DTOs/CreateAddressDto.cs
namespace SaborExpress.Modules.Addresses.DTOs
{
    // CustomerId sale del usuario autenticado, no del body.
    public class CreateAddressDto
    {
        public string AddressLine { get; set; } = string.Empty;
        public string? Reference { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public bool IsDefault { get; set; }
    }
}
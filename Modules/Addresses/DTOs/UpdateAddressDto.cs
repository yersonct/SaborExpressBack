// Modules/Addresses/DTOs/UpdateAddressDto.cs
namespace SaborExpress.Modules.Addresses.DTOs
{
    public class UpdateAddressDto
    {
        public string AddressLine { get; set; } = string.Empty;
        public string? Reference { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
    }
}
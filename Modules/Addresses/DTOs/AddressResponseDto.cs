// Modules/Addresses/DTOs/AddressResponseDto.cs
namespace SaborExpress.Modules.Addresses.DTOs
{
    public class AddressResponseDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string AddressLine { get; set; } = string.Empty;
        public string? Reference { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public bool IsDefault { get; set; }
    }
}
// Modules/Addresses/Validators/AddressValidator.cs
using SaborExpress.Modules.Addresses.DTOs;

namespace SaborExpress.Modules.Addresses.Validators
{
    public class AddressValidator
    {
        public void ValidateCreate(CreateAddressDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.AddressLine))
                throw new ArgumentException("La dirección es obligatoria");

            ValidateCoordinates(dto.Latitude, dto.Longitude);
        }

        public void ValidateUpdate(UpdateAddressDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.AddressLine))
                throw new ArgumentException("La dirección es obligatoria");

            ValidateCoordinates(dto.Latitude, dto.Longitude);
        }

        private void ValidateCoordinates(decimal latitude, decimal longitude)
        {
            if (latitude < -90 || latitude > 90)
                throw new ArgumentException("La latitud debe estar entre -90 y 90");

            if (longitude < -180 || longitude > 180)
                throw new ArgumentException("La longitud debe estar entre -180 y 180");
        }
    }
}
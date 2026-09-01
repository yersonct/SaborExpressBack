// Modules/Addresses/Services/AddressService.cs
using SaborExpress.Modules.Addresses.DTOs;
using SaborExpress.Modules.Addresses.Interfaces;
using SaborExpress.Modules.Addresses.Mappings;
using SaborExpress.Modules.Addresses.Models;
using SaborExpress.Modules.Addresses.Validators;
using SaborExpress.Modules.Auth.Interfaces;

namespace SaborExpress.Modules.Addresses.Services
{
    public class AddressService : IAddressService
    {
        private readonly IAddressRepository _addressRepository;
        private readonly AddressValidator _validator;
        private readonly IAuthRepository _authRepository;

        public AddressService(
            IAddressRepository addressRepository,
            AddressValidator validator,
            IAuthRepository authRepository)
        {
            _addressRepository = addressRepository;
            _validator = validator;
            _authRepository = authRepository;
        }

        // Solo el dueño puede listar todas sus direcciones.
        // (Un Repartidor no necesita ver la lista completa de alguien, solo
        // la dirección puntual de su entrega asignada — eso es GetByIdAsync.)
        public async Task<List<AddressResponseDto>> GetByCustomerIdAsync(int customerId, int currentUserId)
        {
            var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

            var isOwner = currentUser.Customer?.Id == customerId;
            if (!isOwner)
                throw new UnauthorizedAccessException("Solo puedes ver tus propias direcciones");

            var addresses = await _addressRepository.GetByCustomerIdAsync(customerId);
            return addresses.Select(AddressMapper.ToResponse).ToList();
        }

        // Dueño de la dirección, o Repartidor con una entrega asignada a esa dirección.
        public async Task<AddressResponseDto> GetByIdAsync(int id, int currentUserId)
        {
            var address = await _addressRepository.GetByIdAsync(id);
            if (address == null)
                throw new ArgumentException("La dirección no existe");

            var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

            var isOwner = currentUser.Customer?.Id == address.CustomerId;
            if (isOwner)
                return AddressMapper.ToResponse(address);

            if (currentUser.Employee != null)
            {
                var isAssignedDeliveryPerson = await _addressRepository
                    .IsAssignedToDeliveryPersonAsync(id, currentUser.Employee.Id);

                if (isAssignedDeliveryPerson)
                    return AddressMapper.ToResponse(address);
            }

            throw new UnauthorizedAccessException("No tienes permiso para ver esta dirección");
        }

        public async Task<AddressResponseDto> CreateAsync(CreateAddressDto dto, int customerId)
        {
            _validator.ValidateCreate(dto);

            var address = new Address
            {
                CustomerId = customerId,
                AddressLine = dto.AddressLine,
                Reference = dto.Reference,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                IsDefault = dto.IsDefault
            };

            if (dto.IsDefault)
                await _addressRepository.ClearDefaultForCustomerAsync(customerId);

            await _addressRepository.AddAsync(address);
            await _addressRepository.SaveChangesAsync();

            return AddressMapper.ToResponse(address);
        }

        public async Task<AddressResponseDto> UpdateAsync(int id, UpdateAddressDto dto, int customerId)
        {
            var address = await _addressRepository.GetByIdAsync(id);
            if (address == null)
                throw new ArgumentException("La dirección no existe");

            if (address.CustomerId != customerId)
                throw new ArgumentException("Esta dirección no pertenece al cliente");

            _validator.ValidateUpdate(dto);

            address.AddressLine = dto.AddressLine;
            address.Reference = dto.Reference;
            address.Latitude = dto.Latitude;
            address.Longitude = dto.Longitude;

            await _addressRepository.UpdateAsync(address);
            await _addressRepository.SaveChangesAsync();

            return AddressMapper.ToResponse(address);
        }

        public async Task<AddressResponseDto> SetDefaultAsync(int id, int customerId)
        {
            var address = await _addressRepository.GetByIdAsync(id);
            if (address == null)
                throw new ArgumentException("La dirección no existe");

            if (address.CustomerId != customerId)
                throw new ArgumentException("Esta dirección no pertenece al cliente");

            if (address.IsDefault)
                throw new ArgumentException("Esta dirección ya es la predeterminada");

            await _addressRepository.ClearDefaultForCustomerAsync(customerId, address.Id);
            address.IsDefault = true;

            await _addressRepository.UpdateAsync(address);
            await _addressRepository.SaveChangesAsync();

            return AddressMapper.ToResponse(address);
        }

        public async Task DeleteAsync(int id, int customerId)
        {
            var address = await _addressRepository.GetByIdAsync(id);
            if (address == null)
                throw new ArgumentException("La dirección no existe");

            if (address.CustomerId != customerId)
                throw new ArgumentException("Esta dirección no pertenece al cliente");

            await _addressRepository.DeleteAsync(address);
            await _addressRepository.SaveChangesAsync();
        }
    }
}
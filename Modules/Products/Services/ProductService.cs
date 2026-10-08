using SaborExpress.Modules.Products.DTOs;
using SaborExpress.Modules.Products.Interfaces;
using SaborExpress.Modules.Products.Mappings;
using SaborExpress.Modules.Products.Validators;
using SaborExpress.Shared.Constants;
using SaborExpress.Shared.Interfaces;
using Microsoft.AspNetCore.Http;

namespace SaborExpress.Modules.Products.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repository;
        private readonly ProductValidator _validator;
        private readonly IAuthorizationService _authorizationService;

        public ProductService(
            IProductRepository repository,
            ProductValidator validator,
            IAuthorizationService authorizationService)
        {
            _repository = repository;
            _validator = validator;
            _authorizationService = authorizationService;
        }

        public async Task<List<ProductResponseDto>> GetAllAsync(int? branchId = null)
        {
            var products = await _repository.GetAllAsync(branchId);
            return products.Select(p => p.ToResponseDto()).ToList();
        }

        public async Task<ProductResponseDto?> GetByIdAsync(int id)
        {
            var product = await _repository.GetByIdAsync(id);
            return product?.ToResponseDto();
        }

        public async Task<ProductResponseDto> CreateAsync(ProductCreateDto dto)
        {
            await _validator.ValidateCreateAsync(dto);

            string? photoPath = null;
            if (dto.Photo != null)
                photoPath = await SavePhotoAsync(dto.Photo);

            var entity = dto.ToEntity(photoPath);
            var created = await _repository.AddAsync(entity);
            return created.ToResponseDto();
        }

        public async Task UpdateAsync(int id, ProductUpdateDto dto)
        {
            var product = await _repository.GetByIdAsync(id)
                ?? throw new ArgumentException("Producto no encontrado");

            await _validator.ValidateUpdateAsync(id, dto);

            string? photoPath = null;
            if (dto.Photo != null)
                photoPath = await SavePhotoAsync(dto.Photo);

            product.ApplyUpdate(dto, photoPath);
            await _repository.UpdateAsync(product);
        }

        public async Task DeleteAsync(int id)
        {
            var product = await _repository.GetByIdAsync(id)
                ?? throw new ArgumentException("Producto no encontrado");

            var hasOrders = await _repository.HasOrderItemsAsync(id);
            if (hasOrders)
                throw new InvalidOperationException(
                    "No se puede eliminar este producto porque tiene pedidos asociados. " +
                    "Usa Actualizar para desactivarlo en su lugar.");

            await _repository.DeleteAsync(product);
        }

        private async Task<string> SavePhotoAsync(IFormFile photo)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(photo.FileName)}";
            var folderPath = Path.Combine("wwwroot", "uploads", "products");
            Directory.CreateDirectory(folderPath);
            var filePath = Path.Combine(folderPath, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await photo.CopyToAsync(stream);

            return $"/uploads/products/{fileName}";
        }

        public async Task<List<ProductResponseDto>> GetByCategoryIdAsync(int categoryId)
        {
            var products = await _repository.GetByCategoryIdAsync(categoryId);
            return products.Select(p => p.ToResponseDto()).ToList();
        }

        // Ahora recibe el empleado actual y valida el permiso ActivarDesactivarPlato
        // (Gerente/Administrador ya entran por el rol en el controller; el Cocinero
        // necesita este permiso + turno activo para llegar hasta aquí).
        public async Task UpdateStatusAsync(int id, bool status, int currentEmployeeId, bool isManagerOrAdmin)
        {
            var product = await _repository.GetByIdAsync(id)
                ?? throw new ArgumentException("Producto no encontrado");

            if (!isManagerOrAdmin)
            {
                var canToggle = await _authorizationService.CanPerformActionAsync(currentEmployeeId, PermissionNames.ActivarDesactivarPlato);
                if (!canToggle)
                    throw new InvalidOperationException("No tienes permiso para activar/desactivar productos ahora mismo.");
            }

            product.Status = status;
            await _repository.UpdateAsync(product);
        }
    }
}
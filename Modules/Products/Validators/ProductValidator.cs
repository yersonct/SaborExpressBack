using SaborExpress.Modules.Categories.Interfaces;
using SaborExpress.Modules.Products.DTOs;
using SaborExpress.Modules.Products.Interfaces;

namespace SaborExpress.Modules.Products.Validators
{
    public class ProductValidator
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private const int MaxNameLength = 100;
        private const int MaxDescriptionLength = 500;
        private const long MaxPhotoSizeBytes = 5 * 1024 * 1024;
        private static readonly string[] AllowedPhotoTypes = { "image/jpeg", "image/png", "image/webp" };

        public ProductValidator(IProductRepository productRepository, ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }

        public async Task ValidateCreateAsync(ProductCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("El nombre del producto es obligatorio");

            if (dto.Name.Length > MaxNameLength)
                throw new ArgumentException($"El nombre no debe superar {MaxNameLength} caracteres");

            if (!string.IsNullOrWhiteSpace(dto.Description) && dto.Description.Length > MaxDescriptionLength)
                throw new ArgumentException($"La descripción no debe superar {MaxDescriptionLength} caracteres");

            if (dto.Price <= 0)
                throw new ArgumentException("El precio debe ser mayor a cero");

            if (dto.PreparationTimeInMinutes < 0)
                throw new ArgumentException("El tiempo de preparación no puede ser negativo");

            if (await _categoryRepository.GetByIdAsync(dto.CategoryId) is null)
                throw new ArgumentException("La categoría seleccionada no existe");

            if (await _productRepository.ExistsByNameAsync(dto.Name))
                throw new ArgumentException("Ya existe un producto con ese nombre");

            ValidatePhoto(dto.Photo);
        }

        public async Task ValidateUpdateAsync(int id, ProductUpdateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("El nombre del producto es obligatorio");

            if (dto.Name.Length > MaxNameLength)
                throw new ArgumentException($"El nombre no debe superar {MaxNameLength} caracteres");

            if (!string.IsNullOrWhiteSpace(dto.Description) && dto.Description.Length > MaxDescriptionLength)
                throw new ArgumentException($"La descripción no debe superar {MaxDescriptionLength} caracteres");

            if (dto.Price <= 0)
                throw new ArgumentException("El precio debe ser mayor a cero");

            if (dto.PreparationTimeInMinutes < 0)
                throw new ArgumentException("El tiempo de preparación no puede ser negativo");

            if (await _categoryRepository.GetByIdAsync(dto.CategoryId) is null)
                throw new ArgumentException("La categoría seleccionada no existe");

            if (await _productRepository.ExistsByNameAsync(dto.Name, excludeId: id))
                throw new ArgumentException("Ya existe otro producto con ese nombre");

            ValidatePhoto(dto.Photo);
        }

        private void ValidatePhoto(Microsoft.AspNetCore.Http.IFormFile? photo)
        {
            if (photo == null) return;

            if (!AllowedPhotoTypes.Contains(photo.ContentType))
                throw new ArgumentException("La foto debe ser JPG, PNG o WEBP");

            if (photo.Length > MaxPhotoSizeBytes)
                throw new ArgumentException("La foto no debe superar 5MB");
        }
    }
}
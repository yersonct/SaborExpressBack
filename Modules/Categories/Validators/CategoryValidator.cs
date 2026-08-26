using SaborExpress.Modules.Categories.DTOs;
using SaborExpress.Modules.Categories.Interfaces;

namespace SaborExpress.Modules.Categories.Validators
{
    public class CategoryValidator
    {
        private readonly ICategoryRepository _categoryRepository;
        private const int MaxNameLength = 100;
        private const int MaxDescriptionLength = 500;

        public CategoryValidator(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task ValidateCreateAsync(CategoryCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("El nombre de la categoría es obligatorio");

            if (dto.Name.Length > MaxNameLength)
                throw new ArgumentException($"El nombre no debe superar {MaxNameLength} caracteres");

            if (!string.IsNullOrWhiteSpace(dto.Description) && dto.Description.Length > MaxDescriptionLength)
                throw new ArgumentException($"La descripción no debe superar {MaxDescriptionLength} caracteres");

            if (await _categoryRepository.ExistsByNameAsync(dto.Name))
                throw new ArgumentException("Ya existe una categoría con ese nombre");
        }

        public async Task ValidateUpdateAsync(int id, CategoryUpdateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new ArgumentException("El nombre de la categoría es obligatorio");

            if (dto.Name.Length > MaxNameLength)
                throw new ArgumentException($"El nombre no debe superar {MaxNameLength} caracteres");

            if (!string.IsNullOrWhiteSpace(dto.Description) && dto.Description.Length > MaxDescriptionLength)
                throw new ArgumentException($"La descripción no debe superar {MaxDescriptionLength} caracteres");

            if (await _categoryRepository.ExistsByNameAsync(dto.Name, excludeId: id))
                throw new ArgumentException("Ya existe otra categoría con ese nombre");
        }

        public void ValidateDelete(bool hasActiveProducts)
        {
            if (hasActiveProducts)
                throw new ArgumentException("No se puede eliminar una categoría que tiene productos activos");
        }
    }
}
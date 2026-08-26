using SaborExpress.Modules.Categories.DTOs;
using SaborExpress.Modules.Categories.Interfaces;
using SaborExpress.Modules.Categories.Mappings;
using SaborExpress.Modules.Categories.Validators;
using Microsoft.EntityFrameworkCore;

namespace SaborExpress.Modules.Categories.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _repository;
        private readonly CategoryValidator _validator;

        public CategoryService(ICategoryRepository repository, CategoryValidator validator)
        {
            _repository = repository;
            _validator = validator;
        }

        public async Task<List<CategoryResponseDto>> GetAllAsync()
        {
            var categories = await _repository.GetAllAsync();
            return categories.Select(c => c.ToResponseDto()).ToList();
        }

        public async Task<CategoryResponseDto?> GetByIdAsync(int id)
        {
            var category = await _repository.GetByIdAsync(id);
            return category?.ToResponseDto();
        }

        public async Task<CategoryResponseDto> CreateAsync(CategoryCreateDto dto)
        {
            await _validator.ValidateCreateAsync(dto);
            var entity = dto.ToEntity();
            var created = await _repository.AddAsync(entity);
            return created.ToResponseDto();
        }

        public async Task UpdateAsync(int id, CategoryUpdateDto dto)
        {
            var category = await _repository.GetByIdAsync(id)
                ?? throw new ArgumentException("Categoría no encontrada");

            await _validator.ValidateUpdateAsync(id, dto);
            category.ApplyUpdate(dto);

            // Nota: como GetByIdAsync ya devuelve la entidad rastreada por EF (FindAsync),
            // este UpdateAsync es técnicamente redundante — EF ya detecta el cambio solo.
            // Se deja explícito de todos modos por claridad de intención en el código.
            await _repository.UpdateAsync(category);
        }

        public async Task DeleteAsync(int id)
        {
            var category = await _repository.GetByIdAsync(id)
                ?? throw new ArgumentException("Categoría no encontrada");

            var hasActiveProducts = await _repository.HasActiveProductsAsync(id);
            _validator.ValidateDelete(hasActiveProducts);

            try
            {
                await _repository.DeleteAsync(category);
            }
            catch (DbUpdateException)
            {
                throw new InvalidOperationException(
                    "No se puede eliminar esta categoría porque tiene productos asociados (incluso inactivos). " +
                    "Usa la opción de editar para desactivarla (Status) en su lugar.");
            }
        }
    }
}
using SaborExpress.Modules.Categories.DTOs;
using SaborExpress.Modules.Categories.Interfaces;
using SaborExpress.Modules.Categories.Mappings;
using SaborExpress.Modules.Categories.Validators;
using Microsoft.EntityFrameworkCore;
using SaborExpress.Shared.Interfaces;

namespace SaborExpress.Modules.Categories.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _repository;
        private readonly CategoryValidator _validator;
        private readonly IRealtimeNotifier _realtime;

        public CategoryService(
            ICategoryRepository repository,
            CategoryValidator validator,
            IRealtimeNotifier realtime)
        {
            _repository = repository;
            _validator = validator;
            _realtime = realtime;
        }

        public async Task<List<CategoryResponseDto>> GetAllAsync(bool includeInactive = false)
        {
            var categories = await _repository.GetAllAsync(includeInactive);
            return categories.Select(c => c.ToResponseDto()).ToList();
        }

        public async Task<CategoryResponseDto?> GetByIdAsync(int id, bool includeInactive = false)
        {
            var category = await _repository.GetByIdAsync(id);
            if (!includeInactive && category is { Status: false })
                return null;

            return category?.ToResponseDto();
        }

        public async Task<CategoryResponseDto> CreateAsync(CategoryCreateDto dto)
        {
            await _validator.ValidateCreateAsync(dto);
            var entity = dto.ToEntity();
            var created = await _repository.AddAsync(entity);
            await _realtime.ProductsChangedAsync();
            await _realtime.PublicMenuChangedAsync();
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
            await _realtime.ProductsChangedAsync();
            await _realtime.PublicMenuChangedAsync();
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
                await _realtime.ProductsChangedAsync();
                await _realtime.PublicMenuChangedAsync();
            }
            catch (DbUpdateException)
            {
                throw new SaborExpress.Shared.Exceptions.ConflictException(
                    "No se puede eliminar esta categoría porque tiene productos asociados (incluso inactivos). " +
                    "Usa la opción de editar para desactivarla (Status) en su lugar.");
            }
        }
    }
}

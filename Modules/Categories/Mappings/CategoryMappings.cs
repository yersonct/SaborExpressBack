using SaborExpress.Modules.Categories.DTOs;
using SaborExpress.Modules.Categories.Models;

namespace SaborExpress.Modules.Categories.Mappings
{
    public static class CategoryMappings
    {
        public static CategoryResponseDto ToResponseDto(this Category category)
        {
            return new CategoryResponseDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Status = category.Status,
                CreatedAt = category.CreatedAt
            };
        }

        public static Category ToEntity(this CategoryCreateDto dto)
        {
            return new Category
            {
                Name = dto.Name,
                Description = dto.Description,
                Status = true,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static void ApplyUpdate(this Category category, CategoryUpdateDto dto)
        {
            category.Name = dto.Name;
            category.Description = dto.Description;
            category.Status = dto.Status;
        }
    }
}
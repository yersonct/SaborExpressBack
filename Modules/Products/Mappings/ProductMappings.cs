using SaborExpress.Modules.Products.DTOs;
using SaborExpress.Modules.Products.Models;

namespace SaborExpress.Modules.Products.Mappings
{
    public static class ProductMappings
    {
        public static ProductResponseDto ToResponseDto(this Product product)
        {
            return new ProductResponseDto
            {
                Id = product.Id,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name ?? string.Empty,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Photo = product.Photo,
                PreparationTimeInMinutes = product.PreparationTimeInMinutes,
                Status = product.Status,
                CreatedAt = product.CreatedAt
            };
        }

        public static Product ToEntity(this ProductCreateDto dto, string? photoPath)
        {
            return new Product
            {
                CategoryId = dto.CategoryId,
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Photo = photoPath,
                PreparationTimeInMinutes = dto.PreparationTimeInMinutes,
                Status = true,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static void ApplyUpdate(this Product product, ProductUpdateDto dto, string? photoPath)
        {
            product.CategoryId = dto.CategoryId;
            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.PreparationTimeInMinutes = dto.PreparationTimeInMinutes;
            product.Status = dto.Status;

            if (photoPath != null)
                product.Photo = photoPath;
        }
    }
}
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
                CategoryNameEn = product.Category?.NameEn,
                CategoryNamePt = product.Category?.NamePt,
                CategoryNameFr = product.Category?.NameFr,
                BranchId = product.BranchId,
                BranchName = product.Branch?.Name,
                Name = product.Name,
                Description = product.Description,
                NameEn = product.NameEn,
                NamePt = product.NamePt,
                NameFr = product.NameFr,
                DescriptionEn = product.DescriptionEn,
                DescriptionPt = product.DescriptionPt,
                DescriptionFr = product.DescriptionFr,
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
                BranchId = dto.BranchId,
                Name = dto.Name,
                Description = dto.Description,
                NameEn = NormalizeTranslation(dto.NameEn),
                NamePt = NormalizeTranslation(dto.NamePt),
                NameFr = NormalizeTranslation(dto.NameFr),
                DescriptionEn = NormalizeTranslation(dto.DescriptionEn),
                DescriptionPt = NormalizeTranslation(dto.DescriptionPt),
                DescriptionFr = NormalizeTranslation(dto.DescriptionFr),
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
            product.BranchId = dto.BranchId;
            product.Name = dto.Name;
            product.Description = dto.Description;
            if (dto.NameEn is not null) product.NameEn = NormalizeTranslation(dto.NameEn);
            if (dto.NamePt is not null) product.NamePt = NormalizeTranslation(dto.NamePt);
            if (dto.NameFr is not null) product.NameFr = NormalizeTranslation(dto.NameFr);
            if (dto.DescriptionEn is not null) product.DescriptionEn = NormalizeTranslation(dto.DescriptionEn);
            if (dto.DescriptionPt is not null) product.DescriptionPt = NormalizeTranslation(dto.DescriptionPt);
            if (dto.DescriptionFr is not null) product.DescriptionFr = NormalizeTranslation(dto.DescriptionFr);
            product.Price = dto.Price;
            product.PreparationTimeInMinutes = dto.PreparationTimeInMinutes;
            product.Status = dto.Status;

            if (photoPath != null)
                product.Photo = photoPath;
        }

        private static string? NormalizeTranslation(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

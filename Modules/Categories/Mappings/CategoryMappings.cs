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
                NameEn = category.NameEn,
                NamePt = category.NamePt,
                NameFr = category.NameFr,
                DescriptionEn = category.DescriptionEn,
                DescriptionPt = category.DescriptionPt,
                DescriptionFr = category.DescriptionFr,
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
                NameEn = NormalizeTranslation(dto.NameEn),
                NamePt = NormalizeTranslation(dto.NamePt),
                NameFr = NormalizeTranslation(dto.NameFr),
                DescriptionEn = NormalizeTranslation(dto.DescriptionEn),
                DescriptionPt = NormalizeTranslation(dto.DescriptionPt),
                DescriptionFr = NormalizeTranslation(dto.DescriptionFr),
                Status = true,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static void ApplyUpdate(this Category category, CategoryUpdateDto dto)
        {
            category.Name = dto.Name;
            category.Description = dto.Description;
            if (dto.NameEn is not null) category.NameEn = NormalizeTranslation(dto.NameEn);
            if (dto.NamePt is not null) category.NamePt = NormalizeTranslation(dto.NamePt);
            if (dto.NameFr is not null) category.NameFr = NormalizeTranslation(dto.NameFr);
            if (dto.DescriptionEn is not null) category.DescriptionEn = NormalizeTranslation(dto.DescriptionEn);
            if (dto.DescriptionPt is not null) category.DescriptionPt = NormalizeTranslation(dto.DescriptionPt);
            if (dto.DescriptionFr is not null) category.DescriptionFr = NormalizeTranslation(dto.DescriptionFr);
            category.Status = dto.Status;
        }

        private static string? NormalizeTranslation(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

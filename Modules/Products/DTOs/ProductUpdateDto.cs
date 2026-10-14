using Microsoft.AspNetCore.Http;

namespace SaborExpress.Modules.Products.DTOs
{
    public class ProductUpdateDto
    {
        public int CategoryId { get; set; }
        public int? BranchId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? NameEn { get; set; }
        public string? NamePt { get; set; }
        public string? NameFr { get; set; }
        public string? DescriptionEn { get; set; }
        public string? DescriptionPt { get; set; }
        public string? DescriptionFr { get; set; }
        public decimal Price { get; set; }
        public IFormFile? Photo { get; set; }
        public int PreparationTimeInMinutes { get; set; }
        public bool Status { get; set; }
    }
}

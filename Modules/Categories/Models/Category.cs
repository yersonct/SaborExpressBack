using SaborExpress.Modules.Products.Models; // AJUSTAR namespace si es distinto

namespace SaborExpress.Modules.Categories.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? NameEn { get; set; }
        public string? NamePt { get; set; }
        public string? NameFr { get; set; }
        public string? DescriptionEn { get; set; }
        public string? DescriptionPt { get; set; }
        public string? DescriptionFr { get; set; }
        public bool Status { get; set; } = true; 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}

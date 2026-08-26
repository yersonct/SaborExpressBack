using SaborExpress.Modules.Products.Models; // AJUSTAR namespace si es distinto

namespace SaborExpress.Modules.Categories.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool Status { get; set; } = true; // Enum Status según tu diagrama; si ya tienes un enum reutilízalo aquí
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Relación 1-N con Product
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
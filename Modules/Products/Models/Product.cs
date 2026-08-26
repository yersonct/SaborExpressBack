using SaborExpress.Modules.Categories.Models;

namespace SaborExpress.Modules.Products.Models
{
    public class Product
    {
        public int Id { get; set; }
        public int CategoryId { get; set; } // FK
        public Category Category { get; set; } = null!;

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Photo { get; set; }
        public int PreparationTimeInMinutes { get; set; }
        public bool Status { get; set; } = true; // Ajusta si ya tienes un enum de estado
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
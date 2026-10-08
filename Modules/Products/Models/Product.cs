using SaborExpress.Modules.Categories.Models;
using SaborExpress.Modules.Branches.Models;

namespace SaborExpress.Modules.Products.Models
{
    public class Product
    {
        public int Id { get; set; }
        public int CategoryId { get; set; } // FK
        public Category Category { get; set; } = null!;

        // Nullable a propósito: null = producto "global" visible en todas las sedes
        // (así los productos que ya existen hoy no se rompen). Si se asigna una
        // sede, el producto solo aparece en esa sede.
        public int? BranchId { get; set; }
        public Branch? Branch { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Photo { get; set; }
        public int PreparationTimeInMinutes { get; set; }
        public bool Status { get; set; } = true; // Ajusta si ya tienes un enum de estado
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
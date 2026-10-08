namespace SaborExpress.Modules.Products.DTOs
{
    public class ProductResponseDto
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;

        // Null = producto global (visible en todas las sedes)
        public int? BranchId { get; set; }
        public string? BranchName { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Photo { get; set; }
        public int PreparationTimeInMinutes { get; set; }
        public bool Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
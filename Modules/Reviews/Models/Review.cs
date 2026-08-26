// Modules/Reviews/Models/Review.cs
using SaborExpress.Modules.Customers.Models;
using SaborExpress.Modules.Orders.Models;

namespace SaborExpress.Modules.Reviews.Models
{
    public class Review
    {
        public int Id { get; set; }

        public int OrderId { get; set; } // FK
        public Order Order { get; set; } = null!;

        public int CustomerId { get; set; } // FK
        public Customer Customer { get; set; } = null!;

        public byte Rating { get; set; } // 1-5
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
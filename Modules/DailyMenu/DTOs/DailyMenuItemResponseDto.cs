using System;

namespace SaborExpress.Modules.DailyMenu.DTOs
{
    public class DailyMenuItemResponseDto
    {
        public int Id { get; set; }
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal ProductPrice { get; set; }
        public DateTime Date { get; set; }
        public string MealPeriod { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

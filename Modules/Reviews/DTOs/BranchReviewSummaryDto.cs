// Modules/Reviews/DTOs/BranchReviewSummaryDto.cs
namespace SaborExpress.Modules.Reviews.DTOs
{
    public class BranchReviewSummaryDto
    {
        public int BranchId { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
    }
}
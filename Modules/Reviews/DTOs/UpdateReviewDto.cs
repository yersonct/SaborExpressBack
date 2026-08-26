// Modules/Reviews/DTOs/UpdateReviewDto.cs
namespace SaborExpress.Modules.Reviews.DTOs
{
    public class UpdateReviewDto
    {
        public byte Rating { get; set; }
        public string? Comment { get; set; }
    }
}
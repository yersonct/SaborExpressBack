// Modules/Reviews/DTOs/ReviewResponseDto.cs
namespace SaborExpress.Modules.Reviews.DTOs
{
    public class ReviewResponseDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public byte Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
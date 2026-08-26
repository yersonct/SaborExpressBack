// Modules/Reviews/DTOs/CreateReviewDto.cs
namespace SaborExpress.Modules.Reviews.DTOs
{
    // CustomerId sale del usuario autenticado, no del body.
    public class CreateReviewDto
    {
        public int OrderId { get; set; }
        public byte Rating { get; set; }
        public string? Comment { get; set; }
    }
}
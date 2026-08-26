// Modules/Reviews/Mappings/ReviewMapper.cs
using SaborExpress.Modules.Reviews.DTOs;
using SaborExpress.Modules.Reviews.Models;

namespace SaborExpress.Modules.Reviews.Mappings
{
    public static class ReviewMapper
    {
        public static ReviewResponseDto ToResponse(Review review)
        {
            return new ReviewResponseDto
            {
                Id = review.Id,
                OrderId = review.OrderId,
                CustomerId = review.CustomerId,
                CustomerName = review.Customer?.Name, // ajusta si tu Customer no tiene "Name"
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt
            };
        }
    }
}
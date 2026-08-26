// Modules/Reviews/Services/ReviewService.cs
using SaborExpress.Modules.Reviews.DTOs;
using SaborExpress.Modules.Reviews.Interfaces;
using SaborExpress.Modules.Reviews.Mappings;
using SaborExpress.Modules.Reviews.Models;
using SaborExpress.Modules.Reviews.Validators;

namespace SaborExpress.Modules.Reviews.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly ReviewValidator _validator;

        public ReviewService(IReviewRepository reviewRepository, ReviewValidator validator)
        {
            _reviewRepository = reviewRepository;
            _validator = validator;
        }

        public async Task<ReviewResponseDto> CreateAsync(CreateReviewDto dto, int customerId)
        {
            await _validator.ValidateCreateAsync(dto, customerId);

            var review = new Review
            {
                OrderId = dto.OrderId,
                CustomerId = customerId,
                Rating = dto.Rating,
                Comment = dto.Comment,
                CreatedAt = DateTime.UtcNow
            };

            await _reviewRepository.AddAsync(review);
            await _reviewRepository.SaveChangesAsync();

            var created = await _reviewRepository.GetByIdAsync(review.Id);
            return ReviewMapper.ToResponse(created!);
        }

        public async Task<ReviewResponseDto> GetByOrderIdAsync(int orderId)
        {
            var review = await _reviewRepository.GetByOrderIdAsync(orderId);
            if (review == null)
                throw new ArgumentException("Este pedido no tiene una reseña");

            return ReviewMapper.ToResponse(review);
        }

        public async Task<List<ReviewResponseDto>> GetByCustomerIdAsync(int customerId)
        {
            var reviews = await _reviewRepository.GetByCustomerIdAsync(customerId);
            return reviews.Select(ReviewMapper.ToResponse).ToList();
        }

        public async Task<List<ReviewResponseDto>> GetByBranchIdAsync(int branchId)
        {
            var reviews = await _reviewRepository.GetByBranchIdAsync(branchId);
            return reviews.Select(ReviewMapper.ToResponse).ToList();
        }

        public async Task DeleteAsync(int id)
        {
            var review = await _reviewRepository.GetByIdAsync(id);
            if (review == null)
                throw new ArgumentException("La reseña no existe");

            await _reviewRepository.DeleteAsync(review);
            await _reviewRepository.SaveChangesAsync();
        }
    }
}
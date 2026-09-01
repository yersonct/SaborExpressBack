// Modules/Reviews/Services/ReviewService.cs
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Reviews.DTOs;
using SaborExpress.Modules.Reviews.Interfaces;
using SaborExpress.Modules.Reviews.Mappings;
using SaborExpress.Modules.Reviews.Models;
using SaborExpress.Modules.Reviews.Validators;
using SaborExpress.Shared.Constants;
using SaborExpress.Shared.Extensions;

namespace SaborExpress.Modules.Reviews.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly ReviewValidator _validator;
        private readonly IAuthRepository _authRepository;

        public ReviewService(
            IReviewRepository reviewRepository,
            ReviewValidator validator,
            IAuthRepository authRepository)
        {
            _reviewRepository = reviewRepository;
            _validator = validator;
            _authRepository = authRepository;
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

        public async Task<ReviewResponseDto> UpdateAsync(int id, UpdateReviewDto dto, int customerId)
        {
            var review = await _reviewRepository.GetByIdAsync(id);
            if (review == null)
                throw new ArgumentException("La reseña no existe");

            _validator.ValidateUpdate(review, dto, customerId);

            review.Rating = dto.Rating;
            review.Comment = dto.Comment;

            await _reviewRepository.UpdateAsync(review);
            await _reviewRepository.SaveChangesAsync();

            var updated = await _reviewRepository.GetByIdAsync(review.Id);
            return ReviewMapper.ToResponse(updated!);
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

        // CAMBIADO: ahora valida que el Administrador solo consulte su propia sede
        public async Task<List<ReviewResponseDto>> GetByBranchIdAsync(int branchId, int currentUserId)
        {
            await EnsureCanAccessBranchAsync(branchId, currentUserId);

            var reviews = await _reviewRepository.GetByBranchIdAsync(branchId);
            return reviews.Select(ReviewMapper.ToResponse).ToList();
        }

        public async Task<BranchReviewSummaryDto> GetBranchSummaryAsync(int branchId)
        {
            var (average, count) = await _reviewRepository.GetBranchSummaryAsync(branchId);

            return new BranchReviewSummaryDto
            {
                BranchId = branchId,
                AverageRating = Math.Round(average, 2),
                TotalReviews = count
            };
        }

        public async Task DeleteAsync(int id, int currentUserId)
        {
            // CAMBIADO: usa GetByIdWithOrderAsync porque ValidateDelete
            // necesita saber review.Order.BranchId
            var review = await _reviewRepository.GetByIdWithOrderAsync(id);
            if (review == null)
                throw new ArgumentException("La reseña no existe");

            var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

            var isGerente = currentUser.HasRole(RoleNames.Gerente);
            var isAdministrador = currentUser.HasRole(RoleNames.Administrador);
            var currentCustomerId = currentUser.Customer?.Id;
            var adminBranchId = currentUser.Employee?.BranchId;

            _validator.ValidateDelete(review, currentCustomerId, isGerente, isAdministrador, adminBranchId);

            await _reviewRepository.DeleteAsync(review);
            await _reviewRepository.SaveChangesAsync();
        }

        // Nuevo: mismo patrón que ya usamos en Orders, Tables, DailyMenu
        private async Task EnsureCanAccessBranchAsync(int branchId, int currentUserId)
        {
            var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

            if (currentUser.HasRole(RoleNames.Gerente))
                return;

            if (currentUser.Employee?.BranchId != branchId)
                throw new InvalidOperationException("Solo puedes ver las reseñas de tu propia sede.");
        }
    }
}
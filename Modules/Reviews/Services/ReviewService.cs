// Modules/Reviews/Services/ReviewService.cs
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Notifications.Enum;
using SaborExpress.Modules.Notifications.Interfaces;
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
        private readonly IUserNotificationService _notificationService;
        private readonly INotificationRepository _notificationRepository;
        private readonly ILogger<ReviewService> _logger;

        public ReviewService(
            IReviewRepository reviewRepository,
            ReviewValidator validator,
            IAuthRepository authRepository,
            IUserNotificationService notificationService,
            INotificationRepository notificationRepository,
            ILogger<ReviewService> logger)
        {
            _reviewRepository = reviewRepository;
            _validator = validator;
            _authRepository = authRepository;
            _notificationService = notificationService;
            _notificationRepository = notificationRepository;
            _logger = logger;
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
            var response = ReviewMapper.ToResponse(created!);

            await NotifyManagersAsync(response);

            return response;
        }

        // Avisa a Gerentes y Administradores de la sede. Si falla, la reseña igual queda guardada.
        private async Task NotifyManagersAsync(ReviewResponseDto review)
        {
            try
            {
                var branchId = await _reviewRepository.GetOrderBranchIdAsync(review.OrderId);
                if (branchId == null) return;

                var recipients = await _notificationRepository.GetReviewRecipientUserIdsAsync(branchId.Value);
                if (recipients.Count == 0) return;

                var customer = string.IsNullOrWhiteSpace(review.CustomerName) ? "Un cliente" : review.CustomerName;
                var message = $"{customer} calificó el pedido #{review.OrderId} con {review.Rating}/5.";

                if (!string.IsNullOrWhiteSpace(review.Comment))
                {
                    var comment = review.Comment.Trim();
                    if (comment.Length > 120) comment = comment[..120] + "...";
                    message += $" Comentario: {comment}";
                }

                await _notificationService.CreateBulkAsync(
                    recipients,
                    "Nueva reseña",
                    message,
                    NotificationType.ReviewReceived,
                    "Order",
                    review.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo notificar la reseña del pedido {OrderId}", review.OrderId);
            }
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
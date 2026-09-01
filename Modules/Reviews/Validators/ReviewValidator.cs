// Modules/Reviews/Validators/ReviewValidator.cs
using SaborExpress.Modules.Reviews.DTOs;
using SaborExpress.Modules.Reviews.Interfaces;
using SaborExpress.Modules.Reviews.Models;

namespace SaborExpress.Modules.Reviews.Validators
{
    public class ReviewValidator
    {
        private readonly IReviewRepository _reviewRepository;

        public ReviewValidator(IReviewRepository reviewRepository)
        {
            _reviewRepository = reviewRepository;
        }

        public async Task ValidateCreateAsync(CreateReviewDto dto, int customerId)
        {
            if (dto.Rating < 1 || dto.Rating > 5)
                throw new ArgumentException("La calificación debe estar entre 1 y 5");

            if (!await _reviewRepository.OrderExistsAndIsReviewableAsync(dto.OrderId))
                throw new ArgumentException(
                    "Solo se pueden calificar pedidos entregados que el cliente hizo por su cuenta desde la app. " +
                    "Los pedidos tomados por un mesero o cajero en el restaurante no se pueden calificar.");

            if (!await _reviewRepository.OrderBelongsToCustomerAsync(dto.OrderId, customerId))
                throw new ArgumentException("Este pedido no pertenece al cliente");

            if (await _reviewRepository.OrderHasReviewAsync(dto.OrderId))
                throw new ArgumentException("Este pedido ya tiene una reseña");
        }

        public void ValidateUpdate(Review review, UpdateReviewDto dto, int customerId)
        {
            if (review.CustomerId != customerId)
                throw new UnauthorizedAccessException("Solo puedes editar tu propia reseña");

            if (dto.Rating < 1 || dto.Rating > 5)
                throw new ArgumentException("La calificación debe estar entre 1 y 5");
        }

        public void ValidateDelete(
            Review review,
            int? currentCustomerId,
            bool isGerente,
            bool isAdministrador,
            int? adminBranchId)
        {
            var isOwner = currentCustomerId.HasValue && review.CustomerId == currentCustomerId.Value;
            if (isOwner)
                return;

            if (isGerente)
                return;

            if (isAdministrador)
            {
                if (adminBranchId.HasValue && adminBranchId.Value == review.Order.BranchId)
                    return;

                throw new UnauthorizedAccessException("Solo puedes borrar reseñas de tu propia sede");
            }

            throw new UnauthorizedAccessException("No tienes permiso para borrar esta reseña");
        }
    }
}
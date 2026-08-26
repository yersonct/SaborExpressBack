// Modules/Payments/Interfaces/IPaymentService.cs
using SaborExpress.Modules.Payments.DTOs;

namespace SaborExpress.Modules.Payments.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentResponseDto> CreateAsync(CreatePaymentDto dto, int cashierId, int currentUserId);
        Task<List<PaymentResponseDto>> GetByOrderIdAsync(int orderId);
        Task<PaymentResponseDto> GetByIdAsync(int id);
        Task<PaymentResponseDto> RefundAsync(int id, RefundPaymentDto dto, int currentUserId);
        Task<List<PaymentResponseDto>> GetAllAsync(PaymentFilterDto filter);
    }
}
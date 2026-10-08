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

        // Nuevos, para Wompi
        Task<WompiWidgetDataDto> InitWompiPaymentAsync(InitWompiPaymentDto dto, int currentUserId);
        Task ProcessWompiWebhookAsync(WompiWebhookDto webhook, string rawBody, string signatureHeader);
        Task<PaymentResponseDto> SyncWompiPaymentAsync(int paymentId, int currentUserId);
        Task<WompiWidgetDataDto> InitCashierWompiPaymentAsync(InitCashierWompiPaymentDto dto, int cashierId, int currentUserId);
        Task SyncPendingWompiPaymentsAsync();

        // NUEVO — cierre de caja del Cajero (solo sus propios pagos de hoy)
        Task<CashierShiftSummaryDto> GetMyShiftSummaryAsync(int cashierId);
    }
}
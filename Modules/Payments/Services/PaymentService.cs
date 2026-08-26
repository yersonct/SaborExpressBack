// Modules/Payments/Services/PaymentService.cs
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Payments.DTOs;
using SaborExpress.Modules.Payments.Enum;
using SaborExpress.Modules.Payments.Interfaces;
using SaborExpress.Modules.Payments.Mappings;
using SaborExpress.Shared.Extensions;
using SaborExpress.Modules.Payments.Models;
using SaborExpress.Modules.Payments.Validators;
using SaborExpress.Shared.Constants;
using SaborExpress.Shared.Interfaces;

namespace SaborExpress.Modules.Payments.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly PaymentValidator _validator;
        private readonly IAuthRepository _authRepository;
        private readonly IAuthorizationService _authorizationService;

        public PaymentService(
            IPaymentRepository paymentRepository,
            PaymentValidator validator,
            IAuthRepository authRepository,
            IAuthorizationService authorizationService)
        {
            _paymentRepository = paymentRepository;
            _validator = validator;
            _authRepository = authRepository;
            _authorizationService = authorizationService;
        }

        public async Task<PaymentResponseDto> CreateAsync(CreatePaymentDto dto, int cashierId, int currentUserId)
        {
            await EnsureCanRegisterPaymentAsync(currentUserId);
            await _validator.ValidateCreateAsync(dto);

            var payment = new Payment
            {
                OrderId = dto.OrderId,
                CashierId = cashierId,
                Method = dto.Method,
                Amount = dto.Amount,
                Status = PaymentStatus.Completed,
                PaidAt = DateTime.UtcNow
            };

            await _paymentRepository.AddAsync(payment);
            await _paymentRepository.SaveChangesAsync();

            var created = await _paymentRepository.GetByIdAsync(payment.Id);
            return PaymentMapper.ToResponse(created!);
        }

        public async Task<List<PaymentResponseDto>> GetByOrderIdAsync(int orderId)
        {
            var payments = await _paymentRepository.GetByOrderIdAsync(orderId);
            return payments.Select(PaymentMapper.ToResponse).ToList();
        }

        public async Task<PaymentResponseDto> GetByIdAsync(int id)
        {
            var payment = await _paymentRepository.GetByIdAsync(id);
            if (payment == null)
                throw new ArgumentException("El pago no existe");

            return PaymentMapper.ToResponse(payment);
        }

        public async Task<PaymentResponseDto> RefundAsync(int id, RefundPaymentDto dto, int currentUserId)
        {
            await EnsureCanRefundAsync(currentUserId);

            var payment = await _paymentRepository.GetByIdAsync(id);
            if (payment == null)
                throw new ArgumentException("El pago no existe");

            _validator.ValidateRefund(payment, dto);

            payment.Status = PaymentStatus.Refunded;

            await _paymentRepository.UpdateAsync(payment);
            await _paymentRepository.SaveChangesAsync();

            var refunded = await _paymentRepository.GetByIdAsync(payment.Id);
            return PaymentMapper.ToResponse(refunded!);
        }

        public async Task<List<PaymentResponseDto>> GetAllAsync(PaymentFilterDto filter)
        {
            var payments = await _paymentRepository.GetAllAsync(filter.BranchId, filter.FromDate, filter.ToDate);
            return payments.Select(PaymentMapper.ToResponse).ToList();
        }

        // Gerente/Administrador: siempre pueden. Cajero: necesita el permiso
        // RegistrarPago y turno activo (validado dentro de CanPerformActionAsync).
        private async Task EnsureCanRegisterPaymentAsync(int currentUserId)
        {
            var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

            if (currentUser.HasRole(RoleNames.Gerente) || currentUser.HasRole(RoleNames.Administrador))
                return;

            var employeeId = currentUser.Employee?.Id
                ?? throw new InvalidOperationException("El usuario actual no tiene un perfil de empleado asociado.");

            var canRegister = await _authorizationService.CanPerformActionAsync(employeeId, PermissionNames.RegistrarPago);
            if (!canRegister)
                throw new InvalidOperationException("No tienes permiso para registrar pagos ahora mismo.");
        }

        // Reembolsar es más sensible: mismo patrón, pero contra el permiso ReembolsarPago.
        private async Task EnsureCanRefundAsync(int currentUserId)
        {
            var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

            if (currentUser.HasRole(RoleNames.Gerente) || currentUser.HasRole(RoleNames.Administrador))
                return;

            var employeeId = currentUser.Employee?.Id
                ?? throw new InvalidOperationException("El usuario actual no tiene un perfil de empleado asociado.");

            var canRefund = await _authorizationService.CanPerformActionAsync(employeeId, PermissionNames.ReembolsarPago);
            if (!canRefund)
                throw new InvalidOperationException("No tienes permiso para procesar reembolsos ahora mismo.");
        }
    }
}
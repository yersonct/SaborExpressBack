// Modules/Payments/Services/PaymentService.cs
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Orders.Enum;
using SaborExpress.Modules.Orders.Interfaces;
using SaborExpress.Modules.Payments.DTOs;
using SaborExpress.Modules.Payments.Enum;
using SaborExpress.Modules.Payments.Interfaces;
using SaborExpress.Modules.Payments.Mappings;
using SaborExpress.Modules.Payments.Models;
using SaborExpress.Modules.Payments.Validators;
using SaborExpress.Modules.Tables.Enum;
using SaborExpress.Modules.Tables.Interfaces;
using SaborExpress.Shared.Constants;
using SaborExpress.Shared.Extensions;
using SaborExpress.Shared.Interfaces;

namespace SaborExpress.Modules.Payments.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly PaymentValidator _validator;
        private readonly IAuthRepository _authRepository;
        private readonly IAuthorizationService _authorizationService;
        private readonly IWompiClient _wompiClient;
        private readonly IOrderRepository _orderRepository;
        private readonly ITableRepository _tableRepository;   // 👈 nuevo

        public PaymentService(
            IPaymentRepository paymentRepository,
            PaymentValidator validator,
            IAuthRepository authRepository,
            IAuthorizationService authorizationService,
            IWompiClient wompiClient,
            IOrderRepository orderRepository,
            ITableRepository tableRepository)                  // 👈 nuevo
        {
            _paymentRepository = paymentRepository;
            _validator = validator;
            _authRepository = authRepository;
            _authorizationService = authorizationService;
            _wompiClient = wompiClient;
            _orderRepository = orderRepository;
            _tableRepository = tableRepository;                // 👈 nuevo
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

            // Nuevo: si con este pago el pedido quedó pagado del todo,
            // liberamos su mesa automáticamente (si tenía una asociada).
            await ReleaseTableIfFullyPaidAsync(dto.OrderId);

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

            // Nota: NO reabrimos la mesa automáticamente al reembolsar —
            // eso sería una decisión de negocio aparte (¿el cliente ya se fue
            // o sigue en la mesa?). Se deja como está, el Mesero decide.

            var refunded = await _paymentRepository.GetByIdAsync(payment.Id);
            return PaymentMapper.ToResponse(refunded!);
        }

        public async Task<List<PaymentResponseDto>> GetAllAsync(PaymentFilterDto filter)
        {
            var payments = await _paymentRepository.GetAllAsync(filter.BranchId, filter.FromDate, filter.ToDate);
            return payments.Select(PaymentMapper.ToResponse).ToList();
        }

        public async Task<WompiWidgetDataDto> InitWompiPaymentAsync(InitWompiPaymentDto dto, int currentUserId)
        {
            var createDto = new CreatePaymentDto
            {
                OrderId = dto.OrderId,
                Method = PaymentMethod.Wompi,
                Amount = dto.Amount
            };
            await _validator.ValidateCreateAsync(createDto);

            var reference = $"SABOREXPRESS-{dto.OrderId}-{Guid.NewGuid():N}";

            var payment = new Payment
            {
                OrderId = dto.OrderId,
                CashierId = null,
                Method = PaymentMethod.Wompi,
                Amount = dto.Amount,
                Status = PaymentStatus.Pending,
                PaidAt = DateTime.UtcNow,
                WompiReference = reference
            };

            await _paymentRepository.AddAsync(payment);
            await _paymentRepository.SaveChangesAsync();

            var amountInCents = (int)(dto.Amount * 100);
            var signature = _wompiClient.BuildIntegritySignature(reference, amountInCents, "COP");

            return new WompiWidgetDataDto
            {
                PaymentId = payment.Id,
                Reference = reference,
                AmountInCents = amountInCents,
                Currency = "COP",
                PublicKey = _wompiClient.PublicKey,
                IntegritySignature = signature
            };
        }

        public async Task ProcessWompiWebhookAsync(WompiWebhookDto webhook, string rawBody, string signatureHeader)
        {
            if (!_wompiClient.VerifyWebhookSignature(rawBody, signatureHeader))
                throw new UnauthorizedAccessException("Firma del webhook inválida.");

            var reference = webhook.Data.Transaction.Reference;
            var payment = await _paymentRepository.GetByWompiReferenceAsync(reference);

            if (payment == null)
                return;

            if (payment.Status == PaymentStatus.Completed || payment.Status == PaymentStatus.Failed)
                return;

            payment.WompiTransactionId = webhook.Data.Transaction.Id;

            switch (webhook.Data.Transaction.Status)
            {
                case "APPROVED":
                    payment.Status = PaymentStatus.Completed;
                    await _paymentRepository.UpdateAsync(payment);
                    await _paymentRepository.SaveChangesAsync();

                    var order = await _orderRepository.GetByIdAsync(payment.OrderId);
                    if (order != null && order.Status == OrderStatus.Pending)
                    {
                        order.Status = OrderStatus.Confirmed;
                        order.UpdatedAt = DateTime.UtcNow;
                        await _orderRepository.UpdateAsync(order);
                        await _orderRepository.SaveChangesAsync();
                    }

                    // Nuevo: mismo chequeo que en CreateAsync — si el pago vía
                    // Wompi completó el total del pedido, liberar su mesa.
                    await ReleaseTableIfFullyPaidAsync(payment.OrderId);
                    break;

                case "DECLINED":
                case "ERROR":
                case "VOIDED":
                    payment.Status = PaymentStatus.Failed;
                    await _paymentRepository.UpdateAsync(payment);
                    await _paymentRepository.SaveChangesAsync();
                    break;
            }
        }

        // Nuevo: revisa si el pedido quedó 100% pagado, y si tiene mesa
        // asociada, la libera automáticamente. No hace nada si falta saldo
        // por cobrar, o si el pedido no tenía mesa (domicilio/para llevar).
        private async Task ReleaseTableIfFullyPaidAsync(int orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null || !order.TableId.HasValue)
                return;

            var payments = await _paymentRepository.GetByOrderIdAsync(orderId);
            var totalPaid = payments
                .Where(p => p.Status == PaymentStatus.Completed)
                .Sum(p => p.Amount);

            if (totalPaid < order.Total)
                return; // todavía falta saldo, no se toca la mesa

            var table = await _tableRepository.GetByIdAsync(order.TableId.Value);
            if (table == null || table.Status != TableStatus.Occupied)
                return; // ya estaba libre, o en otro estado — no forzar el cambio

            table.Status = TableStatus.Available;
            await _tableRepository.UpdateAsync(table);
            await _tableRepository.SaveChangesAsync();
        }

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
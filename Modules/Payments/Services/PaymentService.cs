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
using SaborExpress.Modules.Tables.Interfaces;
using SaborExpress.Shared.Constants;
using SaborExpress.Shared.Extensions;
using SaborExpress.Shared.Interfaces;
using SaborExpress.Shared.Helpers;
using SaborExpress.Modules.Deliveries.Interfaces; 
using SaborExpress.Modules.Notifications.Interfaces;
using SaborExpress.Modules.Notifications.Enum;
using SaborExpress.Modules.Customers.Interfaces;
using SaborExpress.Modules.Employees.Interfaces;
using SaborExpress.Modules.Orders.Models;

namespace SaborExpress.Modules.Payments.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;

        private readonly ILogger<PaymentService> _logger;
        private readonly PaymentValidator _validator;
        private readonly IAuthRepository _authRepository;
        private readonly IAuthorizationService _authorizationService;
        private readonly IDeliveryAssignmentService _deliveryAssignmentService; 
        private readonly IWompiClient _wompiClient;
        private readonly IOrderRepository _orderRepository;
        private readonly ITableAvailabilityService _tableAvailabilityService;
        private readonly IUserNotificationService _notificationService;
        private readonly ICustomerRepository _customerRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IRealtimeNotifier _realtime;

        public PaymentService(
            IPaymentRepository paymentRepository,
            PaymentValidator validator,
            IAuthRepository authRepository,
            IAuthorizationService authorizationService,
            IWompiClient wompiClient,
            IOrderRepository orderRepository,
            ITableAvailabilityService tableAvailabilityService,
            IDeliveryAssignmentService deliveryAssignmentService,
            IUserNotificationService notificationService,
            ICustomerRepository customerRepository,
            IEmployeeRepository employeeRepository,
            ILogger<PaymentService> logger,
            IRealtimeNotifier realtime)
        {
            _realtime = realtime;
            _employeeRepository = employeeRepository;
            _logger = logger;
            _paymentRepository = paymentRepository;
            _validator = validator;
            _authRepository = authRepository;
            _authorizationService = authorizationService;
            _wompiClient = wompiClient;
            _orderRepository = orderRepository;
            _tableAvailabilityService = tableAvailabilityService;
            _deliveryAssignmentService = deliveryAssignmentService;
            _notificationService = notificationService;
            _customerRepository = customerRepository;
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

            if (!await _paymentRepository.TryAddCompletedPaymentAtomicallyAsync(payment))
                throw new InvalidOperationException(
                    "El saldo del pedido cambiÃ³ o ya fue pagado. Actualiza la lista antes de volver a cobrar.");
            await _realtime.PaymentsChangedAsync();
            // El pago es una de las dos confirmaciones requeridas para liberar la mesa.
            await _tableAvailabilityService.TryReleaseAfterConfirmationAsync(dto.OrderId);

            await NotifyPaymentConfirmedAsync(payment);
            await _realtime.OrdersChangedAsync();
            await _realtime.TablesChangedAsync();

            var created = await _paymentRepository.GetByIdAsync(payment.Id);
            return PaymentMapper.ToResponse(created!);
        }

        // Avisa al cajero que registró el pago (si fue él en persona) y
        // adicionalmente lo dejamos preparado para incluir Gerente/Admin de la
        // sede en cuanto tengamos forma de listar sus userIds (ver comentario
        // en NotifyPaymentConfirmedAsync).
        private async Task NotifyPaymentConfirmedAsync(Payment payment)
        {
            if (payment.CashierId != null)
            {
                var cashierUserId = await _authRepository.GetUserIdByEmployeeIdAsync(payment.CashierId.Value);
                if (cashierUserId != null)
                {
                    await _notificationService.CreateAsync(
                        cashierUserId.Value,
                        "Pago confirmado",
                        $"Se registró un pago de ${payment.Amount:N0}.",
                        NotificationType.PaymentConfirmed,
                        relatedEntityType: "Order",
                        relatedEntityId: payment.OrderId,
                        idempotencyKey: payment.Method == PaymentMethod.Wompi
                            ? $"wompi:{payment.Id}:cashier"
                            : null);
                }
            }
            // El aviso de mesa disponible se envía cuando también se confirma
            // que los productos se entregaron al mesero.
            var order = await _orderRepository.GetByIdAsync(payment.OrderId);
            if (order?.EmployeeId != null && order.EmployeeId != payment.CashierId)
            {
                var meseroUserId = await _authRepository.GetUserIdByEmployeeIdAsync(order.EmployeeId.Value);
                if (meseroUserId != null)
                {
                    await _notificationService.CreateAsync(
                        meseroUserId.Value,
                        "Pago registrado",
                        "Se cobró un pedido.",
                        SaborExpress.Modules.Notifications.Enum.NotificationType.PaymentConfirmed,
                        relatedEntityType: "Order",
                        relatedEntityId: payment.OrderId,
                        idempotencyKey: payment.Method == PaymentMethod.Wompi
                            ? $"wompi:{payment.Id}:waiter"
                            : null);
                }
            }
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
            payment.RefundReason = dto.Reason.Trim();

            await _paymentRepository.UpdateAsync(payment);
            await _paymentRepository.SaveChangesAsync();

            await _realtime.PaymentsChangedAsync();
            await _realtime.OrdersChangedAsync();

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

        // NUEVO — cierre de caja: solo lo que ESTE cajero cobró hoy.
        public async Task<CashierShiftSummaryDto> GetMyShiftSummaryAsync(int cashierId)
        {
            // "Hoy" según hora de Colombia (UTC-5), convertido a UTC para comparar con PaidAt
            var todayColombia = ColombiaTime.Now.Date;
            var fromUtc = ColombiaTime.ToUtc(todayColombia);
            var toUtc = ColombiaTime.ToUtc(todayColombia.AddDays(1));
            var payments = await _paymentRepository.GetByCashierInRangeAsync(
                cashierId,
                fromUtc,
                toUtc);

            // Solo cuentan los pagos completados — un reembolso no debe sumar al cuadre de caja.
            var completed = payments.Where(p => p.Status == PaymentStatus.Completed).ToList();

            return new CashierShiftSummaryDto
            {
                TotalCash = completed.Where(p => p.Method == PaymentMethod.Cash).Sum(p => p.Amount),
                TotalCard = completed.Where(p => p.Method == PaymentMethod.Card).Sum(p => p.Amount),
                TotalTransfer = completed
                    .Where(p => p.Method == PaymentMethod.Transfer || p.Method == PaymentMethod.Wompi)
                    .Sum(p => p.Amount),
                TotalAmount = completed.Sum(p => p.Amount),
                PaymentsCount = completed.Count,
                Payments = payments.Select(PaymentMapper.ToResponse).ToList()
            };
        }

        public async Task<WompiWidgetDataDto> InitWompiPaymentAsync(InitWompiPaymentDto dto, int currentUserId)
        {
            var order = await _orderRepository.GetByIdAsync(dto.OrderId)
                ?? throw new ArgumentException("El pedido no existe.");

            var currentUser = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

            // ⚠️ AJUSTA: cómo llegas del usuario a su customerId.
            // Aquí asumo que User tiene la navegación Customer.
            var customerId = currentUser.Customer?.Id;
            if (customerId == null || order.CustomerId != customerId)
                throw new InvalidOperationException("Este pedido no te pertenece.");

            if (order.Status != OrderStatus.Pending)
                throw new InvalidOperationException("Este pedido ya no admite pago en línea.");

            var existing = await _paymentRepository.GetByOrderIdAsync(order.Id);
            if (existing.Any(p => p.Status == PaymentStatus.Completed))
                throw new InvalidOperationException("Este pedido ya está pagado.");

            // El monto SIEMPRE sale del pedido, nunca del cliente
            var amount = order.Total;

            if (amount <= 0)
                throw new InvalidOperationException("El pedido no tiene un total válido.");

            var (payment, created) = await _paymentRepository.GetOrCreatePendingWompiPaymentAsync(
                new Payment
                {
                    OrderId = order.Id,
                    CashierId = null,
                    Method = PaymentMethod.Wompi,
                    Amount = amount,
                    Status = PaymentStatus.Pending,
                    PaidAt = DateTime.UtcNow,
                    WompiReference = $"SABOREXPRESS-{order.Id}-{Guid.NewGuid():N}"
                },
                OrderStatus.Pending);

            if (created)
                await _realtime.PaymentsChangedAsync();

            var amountInCents = (int)Math.Round(payment.Amount * 100m, MidpointRounding.AwayFromZero);
            var signature = _wompiClient.BuildIntegritySignature(payment.WompiReference!, amountInCents, "COP");

            return new WompiWidgetDataDto
            {
                PaymentId = payment.Id,
                Reference = payment.WompiReference!,
                AmountInCents = amountInCents,
                Currency = "COP",
                PublicKey = _wompiClient.PublicKey,
                IntegritySignature = signature
            };
        }
                // Cobro por QR en mesa: lo inicia el cajero, paga el cliente con su celular.
        public async Task<WompiWidgetDataDto> InitCashierWompiPaymentAsync(
            InitCashierWompiPaymentDto dto, int cashierId, int currentUserId)
        {
            await EnsureCanRegisterPaymentAsync(currentUserId);

            var order = await _orderRepository.GetByIdAsync(dto.OrderId)
                ?? throw new ArgumentException("El pedido no existe.");

            var existing = await _paymentRepository.GetByOrderIdAsync(order.Id);
            var alreadyPaid = existing
                .Where(p => p.Status == PaymentStatus.Completed)
                .Sum(p => p.Amount);

            // El monto SIEMPRE lo calcula el back: total menos lo ya pagado
            var amount = order.Total - alreadyPaid;
            if (amount <= 0)
                throw new InvalidOperationException("Este pedido ya está pagado.");

            // Si el cajero vuelve a tocar "QR", reusamos el pago pendiente (mismo QR, sin duplicados)
            var (payment, created) = await _paymentRepository.GetOrCreatePendingWompiPaymentAsync(
                new Payment
                {
                    OrderId = order.Id,
                    CashierId = cashierId,
                    Method = PaymentMethod.Wompi,
                    Amount = amount,
                    Status = PaymentStatus.Pending,
                    PaidAt = DateTime.UtcNow,
                    WompiReference = $"SABOREXPRESS-{order.Id}-{Guid.NewGuid():N}"
                },
                requiredOrderStatus: null);

            if (created)
                await _realtime.PaymentsChangedAsync();

            var amountInCents = (int)Math.Round(payment.Amount * 100m, MidpointRounding.AwayFromZero);
            var signature = _wompiClient.BuildIntegritySignature(payment.WompiReference!, amountInCents, "COP");

            return new WompiWidgetDataDto
            {
                PaymentId = payment.Id,
                Reference = payment.WompiReference!,
                AmountInCents = amountInCents,
                Currency = "COP",
                PublicKey = _wompiClient.PublicKey,
                IntegritySignature = signature
            };
        }
        public async Task ProcessWompiWebhookAsync(WompiWebhookDto webhook, string rawBody, string signatureHeader)
        {
            if (!_wompiClient.VerifyWebhookSignature(rawBody, signatureHeader))
            {
                _logger.LogWarning("Webhook Wompi con firma inválida");
                throw new UnauthorizedAccessException("Firma del webhook inválida.");
            }

            // Solo nos interesan las actualizaciones de transacción
            if (webhook.Event != "transaction.updated")
                return;

            var tx = webhook.Data.Transaction;
            var payment = await _paymentRepository.GetByWompiReferenceAsync(tx.Reference);

            if (payment == null)
            {
                _logger.LogWarning("Webhook Wompi: no existe un pago para la referencia recibida.");
                return;
            }

            // La firma puede NO cubrir transaction.reference: sus propiedades son
            // variables según Wompi. Confirmar la referencia y el ID contra la API
            // autenticada de Wompi antes de modificar pagos de nuestra BD.
            var confirmed = await _wompiClient.GetTransactionByReferenceAsync(tx.Reference);
            if (confirmed == null)
            {
                _logger.LogWarning("No se pudo corroborar el evento Wompi con la API oficial.");
                throw new HttpRequestException("No fue posible verificar la transacción Wompi.");
            }

            if (!string.Equals(confirmed.Reference, tx.Reference, StringComparison.Ordinal) ||
                !string.Equals(confirmed.Id, tx.Id, StringComparison.Ordinal) ||
                !string.Equals(confirmed.Status, tx.Status, StringComparison.Ordinal) ||
                confirmed.AmountInCents != tx.AmountInCents)
            {
                _logger.LogWarning("Evento Wompi firmado no coincide con la transacción consultada.");
                throw new UnauthorizedAccessException("La transacción Wompi no coincide.");
            }

            await ApplyWompiResultAsync(payment, confirmed.Id, confirmed.Status, confirmed.AmountInCents);
        }

        // Respaldo del webhook: el front llama esto mientras hace polling.
        public async Task<PaymentResponseDto> SyncWompiPaymentAsync(int paymentId, int currentUserId)
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId)
                ?? throw new ArgumentException("El pago no existe");

            var user = await _authRepository.GetByIdWithRelationsAsync(currentUserId)
                ?? throw new KeyNotFoundException("Usuario actual no encontrado.");

            // Un cliente solo puede sincronizar sus propios pagos.
            // El personal (cajero, admin, gerente) puede sincronizar los de cualquier pedido.
            if (user.Employee == null && !user.HasRole(RoleNames.Gerente) && !user.HasRole(RoleNames.Administrador))
            {
                if (user.Customer == null || payment.Order.CustomerId != user.Customer.Id)
                    throw new InvalidOperationException("Este pago no te pertenece.");
            }

            if (payment.Status == PaymentStatus.Pending && payment.WompiReference != null)
            {
                var tx = await _wompiClient.GetTransactionByReferenceAsync(payment.WompiReference);
                if (tx != null && string.Equals(tx.Reference, payment.WompiReference,
                    StringComparison.Ordinal))
                    await ApplyWompiResultAsync(payment, tx.Id, tx.Status, tx.AmountInCents);
            }

            var fresh = await _paymentRepository.GetByIdAsync(paymentId);
            return PaymentMapper.ToResponse(fresh!);
        }
                // Consulta a Wompi todos los pagos pendientes recientes (no depende del webhook).
        public async Task SyncPendingWompiPaymentsAsync()
        {
            var pending = await _paymentRepository.GetPendingWompiSinceAsync(DateTime.UtcNow.AddMinutes(-30));
            foreach (var payment in pending)
            {
                try
                {
                    var tx = await _wompiClient.GetTransactionByReferenceAsync(payment.WompiReference!);
                    if (tx != null && string.Equals(tx.Reference, payment.WompiReference,
                        StringComparison.Ordinal))
                        await ApplyWompiResultAsync(payment, tx.Id, tx.Status, tx.AmountInCents);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sincronizando el pago {PaymentId} con Wompi", payment.Id);
                }
            }

            var postProcessing = await _paymentRepository.GetWompiPostProcessingPendingAsync(50);
            foreach (var payment in postProcessing)
            {
                try
                {
                    await ProcessWompiPostProcessingAsync(payment.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "No se pudieron completar los efectos posteriores del pago Wompi {PaymentId}; se reintentará.",
                        payment.Id);
                }
            }
        }
        // Lógica única para aplicar el resultado de Wompi (webhook o sincronización)
        private async Task ApplyWompiResultAsync(Payment payment, string txId, string status, int amountInCents)
        {
            if (payment.Status == PaymentStatus.Completed)
            {
                if (string.Equals(status, "APPROVED", StringComparison.Ordinal))
                    await ProcessWompiPostProcessingAsync(payment.Id);
                return;
            }

            if (payment.Status == PaymentStatus.Failed)
                return;

            // Seguridad: el monto que Wompi cobró debe ser el monto del pago
            var expected = (int)Math.Round(payment.Amount * 100m, MidpointRounding.AwayFromZero);
            if (amountInCents != expected)
            {
                _logger.LogError("Monto Wompi {Got} no coincide con el esperado {Expected} (pago {Id})",
                    amountInCents, expected, payment.Id);
                return;
            }

            var terminalStatus = status switch
            {
                "APPROVED" => PaymentStatus.Completed,
                "DECLINED" or "ERROR" or "VOIDED" => PaymentStatus.Failed,
                _ => PaymentStatus.Pending
            };

            if (terminalStatus == PaymentStatus.Pending)
                return;

            // Compare-and-set SQL. Solamente el proceso ganador dispara efectos
            // posteriores (notificaciones, confirmación del pedido, mesa y SignalR).
            if (!await _paymentRepository.TryTransitionWompiStatusAsync(
                payment.Id, terminalStatus, txId))
            {
                _logger.LogDebug("Resultado Wompi repetido o ya procesado (pago {Id})", payment.Id);
                return;
            }

            _logger.LogInformation("Pago {Id} -> estado Wompi {Status}", payment.Id, status);

            if (terminalStatus == PaymentStatus.Completed)
            {
                await ProcessWompiPostProcessingAsync(payment.Id);
                return;
            }

            await _realtime.PaymentsChangedAsync();
            await _realtime.OrdersChangedAsync();
        }

        private async Task ProcessWompiPostProcessingAsync(int paymentId)
        {
            var nowUtc = DateTime.UtcNow;
            if (!await _paymentRepository.TryClaimWompiPostProcessingAsync(
                paymentId, nowUtc.AddMinutes(-5), nowUtc))
                return;

            try
            {
                var payment = await _paymentRepository.GetByIdAsync(paymentId);
                if (payment == null || payment.Status != PaymentStatus.Completed ||
                    payment.Method != PaymentMethod.Wompi || payment.WompiTransactionId == null)
                {
                    await _paymentRepository.ReleaseWompiPostProcessingClaimAsync(paymentId);
                    return;
                }

                var order = payment.Order;
                if (order.Status == OrderStatus.Pending)
                {
                    order.Status = OrderStatus.Confirmed;
                    order.UpdatedAt = DateTime.UtcNow;
                    await _orderRepository.UpdateAsync(order);
                    await _orderRepository.SaveChangesAsync();
                }

                if (order.OrderType == OrderType.Delivery && order.Status != OrderStatus.Cancelled)
                {
                    await _deliveryAssignmentService.AssignPendingForBranchAsync(order.BranchId);
                    if (payment.CashierId == null)
                        await NotifyPaidDeliveryAsync(order.Id, order.BranchId, payment.Id);
                }

                await _realtime.PaymentsChangedAsync();
                await NotifyCustomerPaymentApprovedAsync(order, payment.Id);
                await _tableAvailabilityService.TryReleaseAfterConfirmationAsync(payment.OrderId);
                await _realtime.OrdersChangedAsync();
                await _realtime.TablesChangedAsync();

                if (payment.CashierId != null)
                    await NotifyPaymentConfirmedAsync(payment);

                await _paymentRepository.MarkWompiPostProcessingCompletedAsync(paymentId, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                try
                {
                    await _paymentRepository.ReleaseWompiPostProcessingClaimAsync(paymentId);
                }
                catch (Exception releaseError)
                {
                    _logger.LogError(releaseError,
                        "No se pudo liberar el reclamo de posprocesamiento del pago {PaymentId}.", paymentId);
                }

                _logger.LogError(ex,
                    "Falló el posprocesamiento del pago Wompi {PaymentId}; queda disponible para reintento.",
                    paymentId);
                throw;
            }
        }
        private async Task NotifyCustomerPaymentApprovedAsync(Order order, int paymentId)
        {
            if (!order.CustomerId.HasValue)
            {
                _logger.LogWarning("Pedido {OrderId} sin CustomerId: no se notifica al cliente.", order.Id);
                return;
            }

            var customerUserId = await _customerRepository.GetUserIdByCustomerIdAsync(order.CustomerId.Value);
            if (customerUserId == null)
            {
                _logger.LogWarning("No se encontrÃ³ UserId para el cliente {CustomerId} (pedido {OrderId}).",
                    order.CustomerId.Value, order.Id);
                return;
            }

            await _notificationService.CreateAsync(
                customerUserId.Value,
                "Pago confirmado",
                "Tu pago fue aprobado.",
                NotificationType.PaymentConfirmed,
                relatedEntityType: "Order",
                relatedEntityId: order.Id,
                idempotencyKey: $"wompi:{paymentId}:customer");

            _logger.LogInformation("NotificaciÃ³n de pago enviada al usuario {UserId} (pedido {OrderId}).",
                customerUserId.Value, order.Id);
        }
        private async Task NotifyPaidDeliveryAsync(int orderId, int branchId, int paymentId)
        {
            var userIds = (await _employeeRepository.GetUserIdsByBranchAndRolesAsync(
                    branchId, RoleNames.Cocinero, RoleNames.Administrador, RoleNames.Cajero))
                .Concat(await _employeeRepository.GetUserIdsByRolesAsync(RoleNames.Gerente))
                .Distinct();

            foreach (var userId in userIds)
            {
                await _notificationService.CreateAsync(
                    userId,
                    "Pedido nuevo",
                    "LlegÃ³ un pedido (ya pagado en lÃ­nea).",
                    NotificationType.OrderCreated,
                    relatedEntityType: "Order",
                    relatedEntityId: orderId,
                    idempotencyKey: $"wompi:{paymentId}:delivery:{userId}");
            }
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

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
        private readonly ITableRepository _tableRepository;   
        private readonly IUserNotificationService _notificationService;
        private readonly ICustomerRepository _customerRepository;
        private readonly IEmployeeRepository _employeeRepository;

        public PaymentService(
            IPaymentRepository paymentRepository,
            PaymentValidator validator,
            IAuthRepository authRepository,
            IAuthorizationService authorizationService,
            IWompiClient wompiClient,
            IOrderRepository orderRepository,
            ITableRepository tableRepository,
            IDeliveryAssignmentService deliveryAssignmentService,
            IUserNotificationService notificationService,
            ICustomerRepository customerRepository,
            IEmployeeRepository employeeRepository,
            ILogger<PaymentService> logger)
        {
            _employeeRepository = employeeRepository;
            _logger = logger;
            _paymentRepository = paymentRepository;
            _validator = validator;
            _authRepository = authRepository;
            _authorizationService = authorizationService;
            _wompiClient = wompiClient;
            _orderRepository = orderRepository;
            _tableRepository = tableRepository;
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

            await _paymentRepository.AddAsync(payment);
            await _paymentRepository.SaveChangesAsync();

            // Nuevo: si con este pago el pedido quedó pagado del todo,
            // liberamos su mesa automáticamente (si tenía una asociada).
            await ReleaseTableIfFullyPaidAsync(dto.OrderId);

            await NotifyPaymentConfirmedAsync(payment);

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
                        $"Se registró un pago de ${payment.Amount:N0} para el pedido #{payment.OrderId}.",
                        NotificationType.PaymentConfirmed,
                        relatedEntityType: "Order",
                        relatedEntityId: payment.OrderId);
                }
            }

            // NUEVO: sin esto, el Mesero nunca se entera de que su mesa se
            // cobró. La mesa SÍ se libera bien en la base de datos
            // (ReleaseTableIfFullyPaidAsync), pero su pantalla no tenía
            // ninguna señal para refrescarse y mostrarla libre.
            var order = await _orderRepository.GetByIdAsync(payment.OrderId);
            if (order?.EmployeeId != null && order.EmployeeId != payment.CashierId)
            {
                var meseroUserId = await _authRepository.GetUserIdByEmployeeIdAsync(order.EmployeeId.Value);
                if (meseroUserId != null)
                {
                    await _notificationService.CreateAsync(
                        meseroUserId.Value,
                        "Pago registrado",
                        $"Se cobró el pedido #{payment.OrderId}.",
                        SaborExpress.Modules.Notifications.Enum.NotificationType.PaymentConfirmed,
                        relatedEntityType: "Order",
                        relatedEntityId: payment.OrderId);
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

        // NUEVO — cierre de caja: solo lo que ESTE cajero cobró hoy.
        public async Task<CashierShiftSummaryDto> GetMyShiftSummaryAsync(int cashierId)
        {
            // "Hoy" según hora de Colombia (UTC-5), convertido a UTC para comparar con PaidAt
            var tz = TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows() ? "SA Pacific Standard Time" : "America/Bogota");
            var nowCo = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            var today = TimeZoneInfo.ConvertTimeToUtc(nowCo.Date, tz);
            var payments = await _paymentRepository.GetByCashierSinceAsync(cashierId, today);

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

            var reference = $"SABOREXPRESS-{order.Id}-{Guid.NewGuid():N}";

            var payment = new Payment
            {
                OrderId = order.Id,
                CashierId = null,
                Method = PaymentMethod.Wompi,
                Amount = amount,
                Status = PaymentStatus.Pending,
                PaidAt = DateTime.UtcNow,
                WompiReference = reference
            };

            await _paymentRepository.AddAsync(payment);
            await _paymentRepository.SaveChangesAsync();

            var amountInCents = (int)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
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
            var payment = existing.FirstOrDefault(p =>
                p.Method == PaymentMethod.Wompi &&
                p.Status == PaymentStatus.Pending &&
                p.Amount == amount &&
                p.WompiReference != null);

            if (payment == null)
            {
                payment = new Payment
                {
                    OrderId = order.Id,
                    CashierId = cashierId,
                    Method = PaymentMethod.Wompi,
                    Amount = amount,
                    Status = PaymentStatus.Pending,
                    PaidAt = DateTime.UtcNow,
                    WompiReference = $"SABOREXPRESS-{order.Id}-{Guid.NewGuid():N}"
                };
                await _paymentRepository.AddAsync(payment);
                await _paymentRepository.SaveChangesAsync();
            }

            var amountInCents = (int)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
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
                _logger.LogWarning("Webhook Wompi: no existe un pago con la referencia {Ref}", tx.Reference);
                return;
            }

            await ApplyWompiResultAsync(payment, tx.Id, tx.Status, tx.AmountInCents);
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
                if (tx != null)
                    await ApplyWompiResultAsync(payment, tx.Id, tx.Status, tx.AmountInCents);
            }

            var fresh = await _paymentRepository.GetByIdAsync(paymentId);
            return PaymentMapper.ToResponse(fresh!);
        }
                // Consulta a Wompi todos los pagos pendientes recientes (no depende del webhook).
        public async Task SyncPendingWompiPaymentsAsync()
        {
            var pending = await _paymentRepository.GetPendingWompiSinceAsync(DateTime.UtcNow.AddMinutes(-30));
            Console.WriteLine($"[SYNC] Pagos Wompi pendientes: {pending.Count}");

            foreach (var payment in pending)
            {
                try
                {
                    var tx = await _wompiClient.GetTransactionByReferenceAsync(payment.WompiReference!);
                    if (tx != null)
                        await ApplyWompiResultAsync(payment, tx.Id, tx.Status, tx.AmountInCents);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sincronizando el pago {PaymentId} con Wompi", payment.Id);
                }
            }
        }
        // Lógica única para aplicar el resultado de Wompi (webhook o sincronización)
        private async Task ApplyWompiResultAsync(Payment payment, string txId, string status, int amountInCents)
        {
            if (payment.Status == PaymentStatus.Completed || payment.Status == PaymentStatus.Failed)
                return;

            // Seguridad: el monto que Wompi cobró debe ser el monto del pago
            var expected = (int)Math.Round(payment.Amount * 100m, MidpointRounding.AwayFromZero);
            if (amountInCents != expected)
            {
                _logger.LogError("Monto Wompi {Got} no coincide con el esperado {Expected} (pago {Id})",
                    amountInCents, expected, payment.Id);
                return;
            }

            payment.WompiTransactionId = txId;
            _logger.LogInformation("Pago {Id} -> estado Wompi {Status}", payment.Id, status);

            switch (status)
            {
                case "APPROVED":
                    payment.Status = PaymentStatus.Completed;
                    await _paymentRepository.UpdateAsync(payment);
                    await _paymentRepository.SaveChangesAsync();

                    var order = await _orderRepository.GetByIdAsync(payment.OrderId);
                    Console.WriteLine($"[SYNC] Pago {payment.Id} APROBADO. Pedido {payment.OrderId} estado actual: {order?.Status}");
                    if (order != null && order.Status == OrderStatus.Pending)
                    {
                        order.Status = OrderStatus.Confirmed;
                        order.UpdatedAt = DateTime.UtcNow;
                        await _orderRepository.UpdateAsync(order);
                        await _orderRepository.SaveChangesAsync();

                        if (order.OrderType == OrderType.Delivery)
                        {
                            try
                            {
                                await _deliveryAssignmentService.AssignAutomaticallyAsync(order.Id);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "No se pudo asignar repartidor al pedido {OrderId}", order.Id);
                            }
                        }

                        // Pago del cliente aprobado: recién ahora se avisa a cocina y caja
                        if (order.OrderType == OrderType.Delivery && payment.CashierId == null)
                            await NotifyPaidDeliveryAsync(order.Id, order.BranchId);
                    }

                    // 1) Primero el cliente: es lo más importante y no debe depender de nada más
                    if (order != null)
                        await NotifyCustomerPaymentApprovedAsync(order);

                    // 2) Liberar mesa (protegido: un fallo aquí no debe tumbar el resto)
                    try
                    {
                        await ReleaseTableIfFullyPaidAsync(payment.OrderId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "No se pudo liberar la mesa del pedido {OrderId}", payment.OrderId);
                    }

                    // 3) Cobro iniciado por un cajero (QR en mesa): avisar al cajero y al mesero
                    if (payment.CashierId != null)
                    {
                        try
                        {
                            await NotifyPaymentConfirmedAsync(payment);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "No se pudo notificar el pago {PaymentId} al cajero/mesero", payment.Id);
                        }
                    }
                    break;

                case "DECLINED":
                case "ERROR":
                case "VOIDED":
                    payment.Status = PaymentStatus.Failed;
                    await _paymentRepository.UpdateAsync(payment);
                    await _paymentRepository.SaveChangesAsync();
                    break;

                // PENDING: no se toca, se espera el siguiente evento
            }
        }
                private async Task NotifyCustomerPaymentApprovedAsync(Order order)
        {
            try
            {
                if (!order.CustomerId.HasValue)
                {
                    _logger.LogWarning("Pedido {OrderId} sin CustomerId: no se notifica al cliente.", order.Id);
                    return;
                }

                var customerUserId = await _customerRepository.GetUserIdByCustomerIdAsync(order.CustomerId.Value);
                if (customerUserId == null)
                {
                    _logger.LogWarning("No se encontró UserId para el cliente {CustomerId} (pedido {OrderId}).",
                        order.CustomerId.Value, order.Id);
                    return;
                }

                await _notificationService.CreateAsync(
                    customerUserId.Value,
                    "Pago confirmado",
                    $"Tu pago para el pedido #{order.Id} fue aprobado.",
                    NotificationType.PaymentConfirmed,
                    relatedEntityType: "Order",
                    relatedEntityId: order.Id);

                _logger.LogInformation("Notificación de pago enviada al usuario {UserId} (pedido {OrderId}).",
                    customerUserId.Value, order.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo notificar el pago aprobado al cliente (pedido {OrderId})", order.Id);
            }
        }
                // Domicilio pagado en línea: avisa a cocina, admin, gerente y cajeros.
        // Es el aviso que antes salía al crear el pedido, aunque nadie hubiera pagado.
        private async Task NotifyPaidDeliveryAsync(int orderId, int branchId)
        {
            try
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
                        $"Llegó el pedido #{orderId} (ya pagado en línea).",
                        NotificationType.OrderCreated,
                        relatedEntityType: "Order",
                        relatedEntityId: orderId);
                }
            }
            catch (Exception ex)
            {
                // El pago ya quedó guardado; un fallo al notificar no debe romperlo.
                _logger.LogError(ex, "No se pudo notificar el pedido pagado {OrderId}", orderId);
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
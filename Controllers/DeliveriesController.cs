// Modules/Deliveries/Controllers/DeliveriesController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.Deliveries.DTOs;
using SaborExpress.Modules.Deliveries.Interfaces;
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Orders.Interfaces;
using SaborExpress.Shared.Constants;
using SaborExpress.Shared.Extensions;
using SaborExpress.Shared.Security;
using System.Security.Claims;

namespace SaborExpress.Modules.Deliveries.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DeliveriesController : ControllerBase
    {
        private readonly IDeliveryService _deliveryService;
        private readonly IDeliveryRepository _deliveryRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IAuthRepository _authRepository;

        public DeliveriesController(
            IDeliveryService deliveryService,
            IDeliveryRepository deliveryRepository,
            IOrderRepository orderRepository,
            IAuthRepository authRepository)
        {
            _deliveryService = deliveryService;
            _deliveryRepository = deliveryRepository;
            _orderRepository = orderRepository;
            _authRepository = authRepository;
        }



        [HttpPost]
        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador}")]
        public async Task<IActionResult> Create([FromBody] CreateDeliveryDto dto)
        {
            var currentEmployeeId = this.GetCurrentEmployeeId();
            var result = await _deliveryService.CreateAsync(dto, currentEmployeeId);
            return CreatedAtAction(nameof(GetByOrderId), new { orderId = result.OrderId }, result);
        }



        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetByOrderId(int orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null) return NotFound();

            var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdValue, out var userId)) return Unauthorized();
            var actor = await _authRepository.GetByIdWithRelationsAsync(userId);
            if (actor == null || !ResourceReadPolicy.CanReadOrder(User, actor, order))
                return Forbid();

            var result = await _deliveryService.GetByOrderIdAsync(orderId);
            return Ok(result);
        }

        [HttpGet("customer/orders/{orderId:int}/tracking")]
        [Authorize(Roles = RoleNames.Cliente)]
        public async Task<IActionResult> GetCustomerTracking(int orderId)
        {
            var customerId = this.GetCurrentCustomerId();
            var result = await _deliveryService.GetCustomerTrackingAsync(orderId, customerId);
            return Ok(result);
        }

        [HttpGet("employee/{deliveryPersonId}")]
        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador},{RoleNames.Repartidor}")]
        public async Task<IActionResult> GetByDeliveryPersonId(int deliveryPersonId)
        {
            var isGerente = User.IsInRole(RoleNames.Gerente);
            var isAdmin = User.IsInRole(RoleNames.Administrador);
            var isRepartidor = User.IsInRole(RoleNames.Repartidor);
            int? currentEmployeeId = null;
            if (!isGerente)
            {
                var employeeIdValue = User.FindFirst("EmployeeId")?.Value;
                if (!int.TryParse(employeeIdValue, out var employeeId)) return Forbid();
                currentEmployeeId = employeeId;
            }

            if (isRepartidor && !isGerente && !isAdmin && deliveryPersonId != currentEmployeeId)
                return StatusCode(403, new { message = "No puedes ver las entregas de otro repartidor" });

            if (isAdmin && !isGerente)
            {
                var adminBranchId = await _deliveryRepository.GetEmployeeBranchIdAsync(currentEmployeeId!.Value);
                var targetBranchId = await _deliveryRepository.GetEmployeeBranchIdAsync(deliveryPersonId);

                if (adminBranchId == null || targetBranchId == null || adminBranchId != targetBranchId)
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "Ese repartidor no pertenece a tu sede" });
            }

            // La app del repartidor solo necesita las entregas activas. Gerencia
            // conserva el historial completo para sus consultas.
            var activeOnly = isRepartidor
                && currentEmployeeId.HasValue
                && deliveryPersonId == currentEmployeeId.Value;
            var result = await _deliveryService.GetByDeliveryPersonIdAsync(deliveryPersonId, activeOnly);
            return Ok(result);
        }

        [HttpGet("branch/{branchId}")]
        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador}")]
        public async Task<IActionResult> GetByBranch(int branchId)
        {
            if (User.IsInRole(RoleNames.Administrador) && !User.IsInRole(RoleNames.Gerente))
            {
                var currentEmployeeId = this.GetCurrentEmployeeId();
                var adminBranchId = await _deliveryRepository.GetEmployeeBranchIdAsync(currentEmployeeId);

                if (adminBranchId != branchId)
                    return StatusCode(StatusCodes.Status403Forbidden, new { message = "Solo puedes ver los repartos de tu propia sede" });
            }

            var result = await _deliveryService.GetByBranchIdAsync(branchId);
            return Ok(result);
        }
                // GET /api/Deliveries/branch/{branchId}/delivery-persons
        [HttpGet("branch/{branchId}/delivery-persons")]
        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador}")]
        public async Task<IActionResult> GetDeliveryPersons(int branchId)
        {
            if (User.IsInRole(RoleNames.Administrador) && !User.IsInRole(RoleNames.Gerente))
            {
                var currentEmployeeId = this.GetCurrentEmployeeId();
                var adminBranchId = await _deliveryRepository.GetEmployeeBranchIdAsync(currentEmployeeId);

                if (adminBranchId != branchId)
                    return StatusCode(403, new { message = "Solo puedes ver los repartidores de tu propia sede" });
            }

            var result = await _deliveryRepository.GetDeliveryPersonsByBranchAsync(branchId);
            return Ok(result);
        }

        [HttpPatch("{id}/status")]
        [Authorize(Roles = $"{RoleNames.Repartidor},{RoleNames.Administrador}")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateDeliveryStatusDto dto)
        {
            var currentEmployeeId = this.GetCurrentEmployeeId();
            var isAdmin = User.IsInRole(RoleNames.Administrador);

            if (isAdmin)
            {
                var delivery = await _deliveryRepository.GetByIdAsync(id);
                if (delivery == null) return NotFound();

                var adminBranchId = await _deliveryRepository.GetEmployeeBranchIdAsync(currentEmployeeId);
                if (!adminBranchId.HasValue || adminBranchId.Value != delivery.Order.BranchId)
                    return Forbid();
            }

            var result = await _deliveryService.UpdateStatusAsync(id, dto, currentEmployeeId, isAdmin);
            return Ok(result);
        }

        // POST /api/Deliveries/{id}/rating — el cliente califica al repartidor
        [HttpPost("{id}/rating")]
        [Authorize(Roles = RoleNames.Cliente)]
        public async Task<IActionResult> Rate(int id, [FromBody] RateDeliveryDto dto)
        {
            var customerId = this.GetCurrentCustomerId();
            var result = await _deliveryService.RateAsync(id, dto, customerId);
            return Ok(result);
        }
    }
}

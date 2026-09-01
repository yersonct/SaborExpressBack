// Modules/Deliveries/Controllers/DeliveriesController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.Deliveries.DTOs;
using SaborExpress.Modules.Deliveries.Interfaces;
using SaborExpress.Shared.Constants;
using SaborExpress.Shared.Extensions;

namespace SaborExpress.Modules.Deliveries.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DeliveriesController : ControllerBase
    {
        private readonly IDeliveryService _deliveryService;
        private readonly IDeliveryRepository _deliveryRepository;

        public DeliveriesController(IDeliveryService deliveryService, IDeliveryRepository deliveryRepository)
        {
            _deliveryService = deliveryService;
            _deliveryRepository = deliveryRepository;
        }

        [HttpGet("available")]
        [Authorize(Roles = RoleNames.Repartidor)]
        public async Task<IActionResult> GetAvailable()
        {
            var currentEmployeeId = this.GetCurrentEmployeeId();
            var result = await _deliveryService.GetAvailableOrdersAsync(currentEmployeeId);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.Repartidor)]
        public async Task<IActionResult> Create([FromBody] CreateDeliveryDto dto)
        {
            var currentEmployeeId = this.GetCurrentEmployeeId();
            var result = await _deliveryService.CreateAsync(dto, currentEmployeeId);
            return CreatedAtAction(nameof(GetByOrderId), new { orderId = result.OrderId }, result);
        }

        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetByOrderId(int orderId)
        {
            var result = await _deliveryService.GetByOrderIdAsync(orderId);
            return Ok(result);
        }

        [HttpGet("employee/{deliveryPersonId}")]
        public async Task<IActionResult> GetByDeliveryPersonId(int deliveryPersonId)
        {
            var currentEmployeeId = this.GetCurrentEmployeeId();
            var isGerente = User.IsInRole(RoleNames.Gerente);
            var isAdmin = User.IsInRole(RoleNames.Administrador);
            var isRepartidor = User.IsInRole(RoleNames.Repartidor);

            if (isRepartidor && !isGerente && !isAdmin && deliveryPersonId != currentEmployeeId)
                return Forbid("No puedes ver las entregas de otro repartidor");

            if (isAdmin && !isGerente)
            {
                var adminBranchId = await _deliveryRepository.GetEmployeeBranchIdAsync(currentEmployeeId);
                var targetBranchId = await _deliveryRepository.GetEmployeeBranchIdAsync(deliveryPersonId);

                if (adminBranchId == null || targetBranchId == null || adminBranchId != targetBranchId)
                    return Forbid("Ese repartidor no pertenece a tu sede");
            }

            var result = await _deliveryService.GetByDeliveryPersonIdAsync(deliveryPersonId);
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
                    return Forbid("Solo puedes ver los repartos de tu propia sede");
            }

            var result = await _deliveryService.GetByBranchIdAsync(branchId);
            return Ok(result);
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateDeliveryStatusDto dto)
        {
            var currentEmployeeId = this.GetCurrentEmployeeId();
            var isAdmin = User.IsInRole(RoleNames.Administrador);

            var result = await _deliveryService.UpdateStatusAsync(id, dto, currentEmployeeId, isAdmin);
            return Ok(result);
        }
    }
}
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.Configurations.DTOs;
using SaborExpress.Modules.Configurations.Interfaces;
using SaborExpress.Shared.Constants;

namespace SaborExpress.Controllers
{
    [ApiController]
    [Route("api/branch-settings")]
    [Authorize]
    public class BranchOperationalSettingsController : ControllerBase
    {
        private readonly IBranchOperationalSettingsService _service;

        public BranchOperationalSettingsController(IBranchOperationalSettingsService service)
            => _service = service;

        // Cualquier usuario autenticado puede leer (el cliente necesita tarifa, IVA y horario).
        // GET /api/branch-settings/{branchId}
        [HttpGet("{branchId:int}")]
        public async Task<IActionResult> Get(int branchId)
            => Ok(await _service.GetAsync(branchId));

        // Gerente: cualquier sede. Administrador: solo la suya (lo valida IBranchAccessGuard).
        // PUT /api/branch-settings/{branchId}
        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador}")]
        [HttpPut("{branchId:int}")]
        public async Task<IActionResult> Update(int branchId, [FromBody] UpdateBranchOperationalSettingsDto dto)
            => Ok(await _service.UpdateAsync(branchId, dto, GetCurrentUserId()));

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("Token inválido: no contiene el identificador del usuario.");
            return int.Parse(claim.Value);
        }
    }
}
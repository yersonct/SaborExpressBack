using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.Configurations.DTOs;
using SaborExpress.Modules.Configurations.Interfaces;
using SaborExpress.Shared.Constants;
using SaborExpress.Shared.Extensions;

namespace SaborExpress.Controllers
{
    [ApiController]
    [Route("api/Configurations")]
    [Authorize]
    public class ConfigurationsController : ControllerBase
    {
        private readonly IConfigurationService _configurationService;

        public ConfigurationsController(IConfigurationService configurationService)
        {
            _configurationService = configurationService;
        }

        [HttpGet]
        [Authorize(Roles = RoleNames.Gerente)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _configurationService.GetAllAsync();
            return Ok(result);
        }

        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador}")]
        [HttpGet("branch/{branchId:int}")]
        public async Task<IActionResult> GetByBranch(int branchId)
        {
            if (User.IsInRole(RoleNames.Administrador) && !User.IsInRole(RoleNames.Gerente))
            {
                var currentEmployeeId = this.GetCurrentEmployeeId();
                var adminBranchId = await _configurationService.GetEmployeeBranchIdAsync(currentEmployeeId);

                if (adminBranchId != branchId)
                    return StatusCode(StatusCodes.Status403Forbidden,
    new { error = "Solo puedes ver la configuración de tu propia sede" });
            }

            var result = await _configurationService.GetByBranchIdAsync(branchId);
            return Ok(result);
        }

        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador}")]
        [HttpGet("key/{key}")]
        public async Task<IActionResult> GetByKey(string key, [FromQuery] int? branchId)
        {
            if (User.IsInRole(RoleNames.Administrador) && !User.IsInRole(RoleNames.Gerente) && branchId.HasValue)
            {
                var currentEmployeeId = this.GetCurrentEmployeeId();
                var adminBranchId = await _configurationService.GetEmployeeBranchIdAsync(currentEmployeeId);

                if (adminBranchId != branchId)
                    return StatusCode(StatusCodes.Status403Forbidden,
    new { error = "Solo puedes ver la configuración de tu propia sede" });
            }

            var result = await _configurationService.GetByKeyAsync(branchId, key);
            return Ok(result);
        }

        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador}")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateConfigurationDto dto)
        {
            if (User.IsInRole(RoleNames.Administrador) && !User.IsInRole(RoleNames.Gerente))
            {
                var currentEmployeeId = this.GetCurrentEmployeeId();
                var adminBranchId = await _configurationService.GetEmployeeBranchIdAsync(currentEmployeeId);

                if (dto.BranchId == null || adminBranchId != dto.BranchId)
                    return Forbid("Solo puedes crear configuración para tu propia sede");
            }

            var result = await _configurationService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetByKey), new { key = result.Key, branchId = result.BranchId }, result);
        }

        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador}")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateConfigurationDto dto)
        {
            if (User.IsInRole(RoleNames.Administrador) && !User.IsInRole(RoleNames.Gerente))
            {
                var currentEmployeeId = this.GetCurrentEmployeeId();
                var adminBranchId = await _configurationService.GetEmployeeBranchIdAsync(currentEmployeeId);
                var targetBranchId = await _configurationService.GetBranchIdByConfigurationIdAsync(id);

                if (targetBranchId == null || adminBranchId != targetBranchId)
                    return Forbid("Solo puedes editar la configuración de tu propia sede");
            }

            var result = await _configurationService.UpdateAsync(id, dto);
            return Ok(result);
        }

        [Authorize(Roles = RoleNames.Gerente)]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _configurationService.DeleteAsync(id);
            return NoContent();
        }
    }
}
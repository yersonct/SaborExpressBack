// Controllers/ConfigurationsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.Configurations.DTOs;
using SaborExpress.Modules.Configurations.Interfaces;

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
        public async Task<IActionResult> GetAll()
        {
            var result = await _configurationService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("branch/{branchId:int}")]
        public async Task<IActionResult> GetByBranch(int branchId)
        {
            var result = await _configurationService.GetByBranchIdAsync(branchId);
            return Ok(result);
        }

        [HttpGet("key/{key}")]
        public async Task<IActionResult> GetByKey(string key, [FromQuery] int? branchId)
        {
            var result = await _configurationService.GetByKeyAsync(branchId, key);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Gerente")]
        public async Task<IActionResult> Create([FromBody] CreateConfigurationDto dto)
        {
            var result = await _configurationService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetByKey), new { key = result.Key, branchId = result.BranchId }, result);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Gerente")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateConfigurationDto dto)
        {
            var result = await _configurationService.UpdateAsync(id, dto);
            return Ok(result);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Gerente")]
        public async Task<IActionResult> Delete(int id)
        {
            await _configurationService.DeleteAsync(id);
            return NoContent();
        }
    }
}
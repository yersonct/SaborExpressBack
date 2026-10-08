using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.Products.DTOs;
using SaborExpress.Modules.Products.Interfaces;
using SaborExpress.Shared.Constants;
using SaborExpress.Shared.Extensions;

namespace SaborExpress.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _service;

        public ProductsController(IProductService service)
        {
            _service = service;
        }

        // 👇 Pública: la usa la carta digital sin login (clientes en las mesas)
        // branchId opcional: si se pasa, filtra productos de esa sede + los globales
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? branchId = null)
        {
            var products = await _service.GetAllAsync(branchId);
            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _service.GetByIdAsync(id);
            if (product is null) return NotFound();
            return Ok(product);
        }

        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador}")]
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] ProductCreateDto dto)
        {
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador}")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromForm] ProductUpdateDto dto)
        {
            await _service.UpdateAsync(id, dto);
            return NoContent();
        }

        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador}")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }

        // 👇 También pública, por si la carta llega a filtrar por categoría
        [AllowAnonymous]
        [HttpGet("category/{categoryId}")]
        public async Task<IActionResult> GetByCategory(int categoryId)
        {
            var products = await _service.GetByCategoryIdAsync(categoryId);
            return Ok(products);
        }

        [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador},{RoleNames.Cocinero}")]
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] ProductStatusUpdateDto dto)
        {
            var isManagerOrAdmin = User.IsInRole(RoleNames.Gerente) || User.IsInRole(RoleNames.Administrador);
            var currentEmployeeId = this.GetCurrentEmployeeId();

            await _service.UpdateStatusAsync(id, dto.Status, currentEmployeeId, isManagerOrAdmin);
            return NoContent();
        }
    }
}
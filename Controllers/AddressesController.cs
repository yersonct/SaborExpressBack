// Controllers/AddressesController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.Addresses.DTOs;
using SaborExpress.Modules.Addresses.Interfaces;
using SaborExpress.Shared.Extensions;

namespace SaborExpress.Controllers
{
    [ApiController]
    [Route("api/Addresses")]
    [Authorize]
    public class AddressesController : ControllerBase
    {
        private readonly IAddressService _addressService;

        public AddressesController(IAddressService addressService)
        {
            _addressService = addressService;
        }

        // GET /api/Addresses/customer/{customerId}
        [HttpGet("customer/{customerId:int}")]
        public async Task<IActionResult> GetByCustomer(int customerId)
        {
            var currentUserId = this.GetCurrentUserId();
            var result = await _addressService.GetByCustomerIdAsync(customerId, currentUserId);
            return Ok(result);
        }

        // GET /api/Addresses/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var currentUserId = this.GetCurrentUserId();
            var result = await _addressService.GetByIdAsync(id, currentUserId);
            return Ok(result);
        }

        // POST /api/Addresses
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAddressDto dto)
        {
            var customerId = this.GetCurrentCustomerId();
            var result = await _addressService.CreateAsync(dto, customerId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        // PUT /api/Addresses/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateAddressDto dto)
        {
            var customerId = this.GetCurrentCustomerId();
            var result = await _addressService.UpdateAsync(id, dto, customerId);
            return Ok(result);
        }

        // PATCH /api/Addresses/{id}/default
        [HttpPatch("{id:int}/default")]
        public async Task<IActionResult> SetDefault(int id)
        {
            var customerId = this.GetCurrentCustomerId();
            var result = await _addressService.SetDefaultAsync(id, customerId);
            return Ok(result);
        }
        // GET /api/Addresses/me/default
        [HttpGet("me/default")]
        public async Task<IActionResult> GetMyDefault()
        {
            var customerId = this.GetCurrentCustomerId();
            var result = await _addressService.GetDefaultForCustomerAsync(customerId);

            if (result == null)
                return NotFound(new { message = "El cliente no tiene una dirección predeterminada." });

            return Ok(result);
        }
        // DELETE /api/Addresses/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var customerId = this.GetCurrentCustomerId();
            await _addressService.DeleteAsync(id, customerId);
            return NoContent();
        }
    }
}
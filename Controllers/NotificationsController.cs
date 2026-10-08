using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaborExpress.Modules.Notifications.DTOs;
using SaborExpress.Modules.Notifications.Interfaces;
using SaborExpress.Shared.Extensions;
using SaborExpress.Shared.Constants;

namespace SaborExpress.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly IUserNotificationService   _service;

        public NotificationsController(IUserNotificationService   service)
        {
            _service = service;
        }

        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetByUser(int userId, [FromQuery] bool? unreadOnly)
        {
            // Un usuario solo puede ver sus propias notificaciones
            if (userId != this.GetCurrentUserId())
                return Forbid();

            var result = await _service.GetByUserAsync(userId, unreadOnly);
            return Ok(result);
        }

        [HttpGet("user/{userId:int}/unread-count")]
        public async Task<IActionResult> GetUnreadCount(int userId)
        {
            if (userId != this.GetCurrentUserId())
                return Forbid();

            var count = await _service.GetUnreadCountAsync(userId);
            return Ok(new { unreadCount = count });
        }

    [HttpPost]
    [Authorize(Roles = $"{RoleNames.Gerente},{RoleNames.Administrador}")]
    public async Task<IActionResult> Create([FromBody] CreateNotificationDto dto)
        {
            var result = await _service.CreateManualAsync(dto);
            return StatusCode(201, result);
        }

        [HttpPatch("{id:int}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var result = await _service.MarkAsReadAsync(id, this.GetCurrentUserId());
            return Ok(result);
        }

        [HttpPatch("user/{userId:int}/read-all")]
        public async Task<IActionResult> MarkAllAsRead(int userId)
        {
            if (userId != this.GetCurrentUserId())
                return Forbid();

            await _service.MarkAllAsReadAsync(userId);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id, this.GetCurrentUserId());
            return NoContent();
        }
    }
}
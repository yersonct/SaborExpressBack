using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SaborExpress.Modules.Employees.Interfaces;
using SaborExpress.Shared.Constants;
using System.Security.Claims;

namespace SaborExpress.Modules.Notifications.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;
        private readonly IEmployeeService _employeeService;

        public NotificationHub(
            ILogger<NotificationHub> logger,
            IEmployeeService employeeService)
        {
            _logger = logger;
            _employeeService = employeeService;
        }

        public async Task UpdateDeliveryLocation(decimal latitude, decimal longitude)
        {
            if (Context.User?.IsInRole(RoleNames.Repartidor) != true)
                throw new HubException("Solo un repartidor puede enviar ubicación.");

            var employeeClaim = Context.User.FindFirst("EmployeeId")?.Value;
            if (!int.TryParse(employeeClaim, out var employeeId) || employeeId <= 0)
                throw new HubException("No se pudo identificar al repartidor.");

            await _employeeService.UpdateMyLocationAsync(employeeId, latitude, longitude);
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            _logger.LogDebug("[Hub] Conexión establecida.");
            if (userId != null)
                await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(userId.Value));

            if (CanReceiveAdminDataUpdates(Context.User))
                await Groups.AddToGroupAsync(Context.ConnectionId, AdminDataUpdatesGroupName);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
                _logger.LogDebug("[Hub] Conexión cerrada ({ErrorType}).", exception?.GetType().Name ?? "Normal");
            if (userId != null)
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(userId.Value));

            await base.OnDisconnectedAsync(exception);
        }

private int? GetUserId()
{
    var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    return int.TryParse(claim, out var id) ? id : null;
}

        public static string GroupName(int userId) => $"user-{userId}";

        public const string AdminDataUpdatesGroupName = "admin-data-updates";

        private static bool CanReceiveAdminDataUpdates(ClaimsPrincipal? user) =>
            user?.IsInRole(RoleNames.Gerente) == true ||
            user?.IsInRole(RoleNames.Administrador) == true;
    }
}

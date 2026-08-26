using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace SaborExpress.Modules.Notifications.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            if (userId != null)
                await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(userId.Value));

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
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
    }
}
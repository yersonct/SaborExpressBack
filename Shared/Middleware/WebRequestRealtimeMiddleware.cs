using System.Globalization;
using Microsoft.AspNetCore.SignalR;
using SaborExpress.Modules.Notifications.Hubs;
using SaborExpress.Shared.Constants;

namespace SaborExpress.Shared.Middleware;

public sealed class WebRequestRealtimeMiddleware
{
    private readonly RequestDelegate _next;

    public WebRequestRealtimeMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        IHubContext<NotificationHub> hub,
        ILogger<WebRequestRealtimeMiddleware> logger)
    {
        if (!ShouldTrack(context))
        {
            await _next(context);
            return;
        }

        var module = GetModule(context.Request.Path);
        var method = context.Request.Method.ToUpperInvariant();
        await _next(context);

        // El cambio ya fue procesado por el controlador. Avisamos a las
        // sesiones del panel inmediatamente, solo cuando la escritura terminó
        // correctamente; la respuesta HTTP no se reemplaza ni se retrasa.
        if (context.Response.StatusCode is >= 200 and < 300)
        {
            if (!HasExistingRealtimeEvent(module))
            {
                await SendSafeAsync(
                    hub, logger, "AdminDataChanged",
                    new { module, method, changedAt = DateTimeOffset.UtcNow });
            }
            if (module == "EmployeeSchedules")
            {
                try { await hub.Clients.All.SendAsync("EmployeeSchedulesChanged"); }
                catch (Exception ex) { logger.LogWarning("Error notificando turnos por SignalR ({ErrorType}).", ex.GetType().Name); }
            }
        }
    }

    private static bool HasExistingRealtimeEvent(string module) =>
        module.ToLowerInvariant() switch
        {
            // These services already publish their focused event after saving.
            "categories" or "products" or "orders" or "orderdetails"
                or "payments" or "tables" or "deliveries" or "reviews" or "notifications"
                or "branchoperationalsettings" => true,
            _ => false
        };

    private static bool ShouldTrack(HttpContext context)
    {
        var request = context.Request;
        var isWriteRequest = HttpMethods.IsPost(request.Method)
            || HttpMethods.IsPut(request.Method)
            || HttpMethods.IsPatch(request.Method)
            || HttpMethods.IsDelete(request.Method);
        // Registrar también mutaciones del móvil y paneles administrativos.
        // Evitar negociar eventos en autenticación o rutas fuera de /api.
        return isWriteRequest
            && context.User.Identity?.IsAuthenticated == true
            && request.Path.StartsWithSegments("/api")
            && !request.Path.StartsWithSegments("/api/auth")
            && !request.Path.StartsWithSegments("/api/notifications")
            && !request.Path.StartsWithSegments("/api/CashClosures");
    }

    private static string GetModule(PathString path)
    {
        var segments = GetNormalizedSegments(path);
        var apiIndex = Array.FindIndex(
            segments,
            segment => string.Equals(segment, "api", StringComparison.OrdinalIgnoreCase));
        var moduleIndex = apiIndex + 1;

        if (moduleIndex < 0 || moduleIndex >= segments.Length)
            return "API";

        var module = segments[moduleIndex];
        var normalizedModule = module.ToLowerInvariant();

        // Several controllers use kebab-case routes instead of
        // /api/[controller]. Give those writes stable module names for the UI.
        return normalizedModule switch
        {
            "branch-settings" => "BranchOperationalSettings",
            "employee-schedules" => "EmployeeSchedules",
            "order-details" => "OrderDetails",
            "role-permissions" => "RolePermissions",
            "user-preferences" => "UserPreferences",
            "user-settings" => "UserSettings",
            "user-roles" => "UserRoles",
            "public" when moduleIndex + 1 < segments.Length
                && string.Equals(segments[moduleIndex + 1], "kitchen-board", StringComparison.OrdinalIgnoreCase)
                    => "Kitchen",
            _ => module
        };
    }

    private static string[] GetNormalizedSegments(PathString path) =>
        (path.Value ?? "")
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(segment => int.TryParse(segment, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)
                || Guid.TryParse(segment, out _)
                    ? "{id}"
                    : Uri.UnescapeDataString(segment))
            .ToArray();

    private static async Task SendSafeAsync(
        IHubContext<NotificationHub> hub,
        ILogger<WebRequestRealtimeMiddleware> logger,
        string eventName,
        object payload)
    {
        try
        {
            await hub.Clients
                .Group(NotificationHub.AdminDataUpdatesGroupName)
                .SendAsync(eventName, payload);
        }
        catch (Exception exception)
        {
            logger.LogWarning("No se pudo emitir el evento en tiempo real ({ErrorType}).", exception.GetType().Name);
        }
    }
}

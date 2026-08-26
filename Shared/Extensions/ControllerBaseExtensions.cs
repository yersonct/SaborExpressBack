using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SaborExpress.Shared.Extensions
{
    public static class ControllerBaseExtensions
    {
        public static int GetCurrentEmployeeId(this ControllerBase controller)
        {
            var claim = controller.User.FindFirst("EmployeeId")?.Value;

            if (!int.TryParse(claim, out var id) || id <= 0)
                throw new UnauthorizedAccessException(
                    "No se pudo identificar al empleado autenticado.");

            return id;
        }

        public static int GetCurrentCustomerId(this ControllerBase controller)
        {
            var claim = controller.User.FindFirst("CustomerId")?.Value;

            if (!int.TryParse(claim, out var id) || id <= 0)
                throw new UnauthorizedAccessException(
                    "No se pudo identificar al cliente autenticado.");

            return id;
        }

        public static int GetCurrentUserId(this ControllerBase controller)
        {
            var claim = controller.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(claim, out var id) || id <= 0)
                throw new UnauthorizedAccessException(
                    "No se pudo identificar al usuario autenticado.");

            return id;
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using SaborExpress.Modules.EmployeeSchedules.Interfaces;
using SaborExpress.Shared.Constants;
using System.Security.Claims;

namespace SaborExpress.Shared.Authorization
{
    public class ActiveShiftHandler : AuthorizationHandler<ActiveShiftRequirement>
    {
        private readonly IEmployeeScheduleService _scheduleService;

        public ActiveShiftHandler(IEmployeeScheduleService scheduleService)
        {
            _scheduleService = scheduleService;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context, ActiveShiftRequirement requirement)
        {
            var user = context.User;

            if (user.IsInRole(RoleNames.Gerente)
                || user.IsInRole(RoleNames.Administrador)
                || user.IsInRole(RoleNames.Cliente))
            {
                context.Succeed(requirement);
                return;
            }

            var employeeIdClaim = user.FindFirst("EmployeeId");
            if (employeeIdClaim == null || !int.TryParse(employeeIdClaim.Value, out var employeeId))
            {
                return;
            }

            var currentShift = await _scheduleService.GetCurrentShiftAsync(employeeId);

            if (currentShift.HasActiveShift)
                context.Succeed(requirement);
        }
    }
}
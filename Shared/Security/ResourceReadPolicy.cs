using System.Security.Claims;
using SaborExpress.Modules.Auth.Models;
using SaborExpress.Modules.Orders.DTOs;
using SaborExpress.Modules.Orders.Models;
using SaborExpress.Shared.Constants;

namespace SaborExpress.Shared.Security;

/// <summary>
/// Control de lectura por rol EFECTIVO (JWT), cliente propietario y sede del empleado.
/// Evita usar User.HasRole() contra la lista histórica de roles del empleado.
/// </summary>
public static class ResourceReadPolicy
{
    private static bool IsCustomerOnly(ClaimsPrincipal identity)
        => identity.IsInRole(RoleNames.Cliente)
           && !identity.IsInRole(RoleNames.Gerente)
           && !identity.IsInRole(RoleNames.Administrador)
           && !identity.IsInRole(RoleNames.Cajero)
           && !identity.IsInRole(RoleNames.Mesero)
           && !identity.IsInRole(RoleNames.Cocinero)
           && !identity.IsInRole(RoleNames.AuxiliarCocina)
           && !identity.IsInRole(RoleNames.Repartidor);

    private static bool IsWaiterOnly(ClaimsPrincipal identity)
        => identity.IsInRole(RoleNames.Mesero)
           && !identity.IsInRole(RoleNames.Gerente)
           && !identity.IsInRole(RoleNames.Administrador)
           && !identity.IsInRole(RoleNames.Cajero)
           && !identity.IsInRole(RoleNames.Cocinero)
           && !identity.IsInRole(RoleNames.AuxiliarCocina)
           && !identity.IsInRole(RoleNames.Repartidor);

    private static bool IsBranchStaff(ClaimsPrincipal identity)
        => identity.IsInRole(RoleNames.Administrador)
           || identity.IsInRole(RoleNames.Cajero)
           || identity.IsInRole(RoleNames.Mesero)
           || identity.IsInRole(RoleNames.Cocinero)
           || identity.IsInRole(RoleNames.AuxiliarCocina);

    public static bool CanReadBranch(ClaimsPrincipal identity, User actor, int branchId)
    {
        if (identity.IsInRole(RoleNames.Gerente)) return true;
        return IsBranchStaff(identity) && actor.Employee?.BranchId == branchId;
    }

    public static bool CanReadOrder(ClaimsPrincipal identity, User actor, Order order)
    {
        if (identity.IsInRole(RoleNames.Gerente)) return true;
        if (identity.IsInRole(RoleNames.Repartidor))
            return actor.Employee != null &&
                   order.AssignedDeliveryPersonId == actor.Employee.Id &&
                   actor.Employee.BranchId == order.BranchId;
        if (IsWaiterOnly(identity))
            return actor.Employee != null && order.EmployeeId == actor.Employee.Id;
        if (IsCustomerOnly(identity))
            return actor.Customer != null && order.CustomerId == actor.Customer.Id;
        return CanReadBranch(identity, actor, order.BranchId);
    }

    public static bool CanReadOrderSummary(ClaimsPrincipal identity, User actor, OrderSummaryDto order)
    {
        if (identity.IsInRole(RoleNames.Gerente)) return true;
        if (identity.IsInRole(RoleNames.Repartidor))
            return actor.Employee != null &&
                   order.AssignedDeliveryPersonId == actor.Employee.Id &&
                   actor.Employee.BranchId == order.BranchId;
        if (IsWaiterOnly(identity))
            return actor.Employee != null && order.EmployeeId == actor.Employee.Id;
        if (IsCustomerOnly(identity))
            return actor.Customer != null && order.CustomerId == actor.Customer.Id;
        return CanReadBranch(identity, actor, order.BranchId);
    }

    public static bool CanReadPaymentOfOrder(ClaimsPrincipal identity, User actor, Order order)
    {
        // Los cocineros no necesitan ver transacciones ni referencias de Wompi.
        bool isFinancialReader = identity.IsInRole(RoleNames.Gerente)
            || identity.IsInRole(RoleNames.Administrador)
            || identity.IsInRole(RoleNames.Cajero)
            || identity.IsInRole(RoleNames.Mesero)
            || identity.IsInRole(RoleNames.Repartidor)
            || IsCustomerOnly(identity);
        return isFinancialReader && CanReadOrder(identity, actor, order);
    }
}

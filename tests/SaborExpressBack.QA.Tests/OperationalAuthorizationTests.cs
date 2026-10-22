using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using SaborExpress.Controllers;
using SaborExpress.Modules.Auth.Models;
using SaborExpress.Modules.Deliveries.Controllers;
using SaborExpress.Modules.Employees.Models;
using SaborExpress.Modules.Orders.Models;
using SaborExpress.Shared.Constants;
using SaborExpress.Shared.Security;
using Xunit;

namespace SaborExpressBack.QA.Tests;

public sealed class OperationalAuthorizationTests
{
    [Fact]
    public void OrdersList_UsesAnExplicitOperationalRoleAllowList()
    {
        AssertRoleSet<OrdersController>("GetAll",
            RoleNames.Gerente, RoleNames.Administrador, RoleNames.Mesero,
            RoleNames.Cajero, "COCINERO", "AUXILIAR_COCINA");
    }

    [Fact]
    public void OrderCustomerCancellation_IsRestrictedToTheCustomerRole()
    {
        AssertRoleSet<OrdersController>("CancelMine", RoleNames.Cliente);
    }

    [Theory]
    [InlineData(RoleNames.Gerente, true)]
    [InlineData(RoleNames.Administrador, true)]
    [InlineData(RoleNames.Mesero, true)]
    [InlineData(RoleNames.Cajero, true)]
    [InlineData("COCINERO", true)]
    [InlineData("AUXILIAR_COCINA", true)]
    [InlineData(RoleNames.Repartidor, false)]
    [InlineData(RoleNames.Cliente, false)]
    public void OperationalOrderList_EnforcesAllowList(string role, bool expected)
    {
        var method = typeof(OrdersController).GetMethod("GetAll");
        Assert.NotNull(method);
        var allowedRoles = method!.GetCustomAttributes<AuthorizeAttribute>()
            .SelectMany(attribute => (attribute.Roles ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(expected, allowedRoles.Contains(role));
    }

    [Fact]
    public void InvoiceCreation_IsLimitedToFinanceRoles()
    {
        AssertRoleSet<InvoicesController>("Create",
            RoleNames.Gerente, RoleNames.Administrador, RoleNames.Cajero);
    }

    [Fact]
    public void PaymentCreation_IsLimitedToFinanceRoles()
    {
        AssertRoleSet<PaymentsController>("Create",
            RoleNames.Gerente, RoleNames.Administrador, RoleNames.Cajero);
    }

    [Fact]
    public void CashClosure_IsLimitedToCashier_AndHistoryToManagement()
    {
        AssertRoleSet<CashClosuresController>("Close", RoleNames.Cajero);
        AssertRoleSet<CashClosuresController>("History", RoleNames.Gerente, RoleNames.Administrador);
    }

    [Fact]
    public void DeliveryAssignmentAndProgress_UseExpectedRoles()
    {
        AssertRoleSet<DeliveriesController>("Create", RoleNames.Gerente, RoleNames.Administrador);
        AssertRoleSet<DeliveriesController>("UpdateStatus", RoleNames.Repartidor, RoleNames.Administrador);
    }

    [Fact]
    public void CashClosureControllerRequiresAuthentication()
    {
        Assert.NotNull(typeof(CashClosuresController).GetCustomAttributes<AuthorizeAttribute>()
            .FirstOrDefault(attribute => attribute.Roles is null));
    }

    [Fact]
    public void BranchRead_IsScopedForAdministratorAndGlobalForManager()
    {
        var actor = new User { Employee = new Employee { BranchId = 10 } };

        Assert.True(ResourceReadPolicy.CanReadBranch(Principal(RoleNames.Administrador), actor, 10));
        Assert.False(ResourceReadPolicy.CanReadBranch(Principal(RoleNames.Administrador), actor, 20));
        Assert.True(ResourceReadPolicy.CanReadBranch(Principal(RoleNames.Gerente), actor, 20));
    }

    [Fact]
    public void OrderRead_IsScopedToWaiterOwnershipAndCourierAssignment()
    {
        var order = new Order { BranchId = 10, EmployeeId = 101, AssignedDeliveryPersonId = 202 };
        var waiter = new User { Employee = new Employee { Id = 101, BranchId = 10 } };
        var otherWaiter = new User { Employee = new Employee { Id = 303, BranchId = 10 } };
        var courier = new User { Employee = new Employee { Id = 202, BranchId = 10 } };
        var otherCourier = new User { Employee = new Employee { Id = 404, BranchId = 10 } };

        Assert.True(ResourceReadPolicy.CanReadOrder(Principal(RoleNames.Mesero), waiter, order));
        Assert.False(ResourceReadPolicy.CanReadOrder(Principal(RoleNames.Mesero), otherWaiter, order));
        Assert.True(ResourceReadPolicy.CanReadOrder(Principal(RoleNames.Repartidor), courier, order));
        Assert.False(ResourceReadPolicy.CanReadOrder(Principal(RoleNames.Repartidor), otherCourier, order));
    }

    [Theory]
    [InlineData(RoleNames.Gerente, true)]
    [InlineData(RoleNames.Administrador, true)]
    [InlineData(RoleNames.Cajero, true)]
    [InlineData(RoleNames.Mesero, true)]
    [InlineData(RoleNames.Repartidor, true)]
    [InlineData(RoleNames.Cocinero, false)]
    public void PaymentRead_IsAllowedOnlyForOperationalFinancialRoles(string role, bool expected)
    {
        var order = new Order { BranchId = 10, EmployeeId = 101, AssignedDeliveryPersonId = 202 };
        var employeeId = role == RoleNames.Repartidor ? 202 : 101;
        var actor = new User { Employee = new Employee { Id = employeeId, BranchId = 10 } };
        var canRead = ResourceReadPolicy.CanReadPaymentOfOrder(Principal(role), actor, order);

        Assert.Equal(expected, canRead);
    }

    private static ClaimsPrincipal Principal(string role) => new(
        new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "qa-test"));

    private static void AssertRoleSet<TController>(string methodName, params string[] expected)
    {
        var method = typeof(TController).GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(method);

        var actual = method!.GetCustomAttributes<AuthorizeAttribute>()
            .SelectMany(attribute => (attribute.Roles ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(expected.ToHashSet(StringComparer.Ordinal), actual);
    }
}

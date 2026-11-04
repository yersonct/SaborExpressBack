using System.Security.Claims;
using Moq;
using SaborExpress.Modules.RolePermissions.DTOs;
using SaborExpress.Modules.RolePermissions.Interfaces;
using SaborExpress.Modules.RolePermissions.Models;
using SaborExpress.Modules.RolePermissions.Services;
using SaborExpress.Shared.Constants;
using SaborExpress.Shared.Exceptions;
using Xunit;

namespace SaborExpressBack.Security.Tests;

public sealed class RolePermissionServiceAuthorizationTests
{
    [Theory]
    [InlineData(RoleNames.Administrador)]
    [InlineData(RoleNames.Mesero)]
    [InlineData(RoleNames.Cajero)]
    [InlineData(RoleNames.Cocinero)]
    [InlineData(RoleNames.AuxiliarCocina)]
    [InlineData(RoleNames.Repartidor)]
    [InlineData(RoleNames.Cliente)]
    public async Task NonManagersCannotMutateGlobalRolePermissions(string role)
    {
        var repository = new Mock<IRolePermissionRepository>();
        var service = new RolePermissionService(repository.Object, rolePermissionValidator: null!);
        var actor = Principal(role);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.AssignAsync(
            new CreateRolePermissionDto { RoleId = 1, PermissionId = 1 }, actor));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.RemoveAsync(1, 1, actor));

        repository.Verify(mock => mock.AddAsync(It.IsAny<RolePermission>()), Times.Never);
        repository.Verify(mock => mock.RemoveAsync(It.IsAny<RolePermission>()), Times.Never);
    }

    [Fact]
    public async Task AnonymousServiceCallsAreRejectedAsUnauthorized()
    {
        var service = new RolePermissionService(
            Mock.Of<IRolePermissionRepository>(), rolePermissionValidator: null!);
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.AssignAsync(
            new CreateRolePermissionDto { RoleId = 1, PermissionId = 1 }, anonymous));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.RemoveAsync(1, 1, anonymous));
    }

    private static ClaimsPrincipal Principal(string role) => new(
        new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], authenticationType: "security-test"));
}

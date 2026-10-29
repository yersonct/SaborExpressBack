using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SaborExpress.Controllers;
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Auth.Models;
using SaborExpress.Modules.Employees.Interfaces;
using SaborExpress.Modules.Employees.Models;
using SaborExpress.Modules.Employees.Services;
using SaborExpress.Modules.Employees.DTOs;
using SaborExpress.Modules.Roles.Models;
using SaborExpress.Modules.UsersRoles.Models;
using SaborExpress.Shared.Constants;
using SaborExpress.Shared.Exceptions;
using SaborExpress.Shared.Interfaces;
using ProjectAuthorizationService = SaborExpress.Shared.Interfaces.IAuthorizationService;
using Xunit;

namespace SaborExpressBack.Security.Tests;

public sealed class EmployeeAccessRegressionTests
{
    private static readonly byte[] SamplePdf = [0x25, 0x50, 0x44, 0x46];

    [Fact]
    public async Task AdminCannotReadCvFromAnotherBranch()
    {
        var (service, _, _, _) = BuildService(
            Admin(10, 1),
            new Employee { Id = 50, BranchId = 2, CvFile = SamplePdf });

        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetCvAsync(50, 10));
    }

    [Fact]
    public async Task AdminCanReadCvFromOwnBranch()
    {
        var (service, _, _, _) = BuildService(
            Admin(10, 1),
            new Employee { Id = 50, BranchId = 1, CvFile = SamplePdf });

        var cv = await service.GetCvAsync(50, 10);
        Assert.NotNull(cv);
        Assert.Equal(SamplePdf, cv.Value.Archivo);
    }

    [Fact]
    public async Task AdminWithoutBranchCannotReadCvWithoutBranch()
    {
        var (service, _, _, _) = BuildService(
            Admin(10, null),
            new Employee { Id = 50, BranchId = null, CvFile = SamplePdf });

        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetCvAsync(50, 10));
    }

    [Fact]
    public async Task ManagerCanReadCvFromAnyBranch()
    {
        var manager = new User
        {
            Id = 10,
            UserRoles = [new UserRole { Role = new Role { Name = RoleNames.Gerente } }]
        };
        var (service, _, _, _) = BuildService(
            manager, new Employee { Id = 50, BranchId = 2, CvFile = SamplePdf });

        var cv = await service.GetCvAsync(50, 10);
        Assert.NotNull(cv);
    }

    [Fact]
    public async Task AdminWithoutBranchCannotListAllEmployees()
    {
        var (service, employees, _, _) = BuildService(
            Admin(10, null), new Employee { Id = 50, BranchId = 2 });

        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAllAsync(10));
        employees.Verify(x => x.GetAllLightAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AdminCannotCreateEmployeeInAnotherBranch()
    {
        var (service, employees, _, _) = BuildService(
            Admin(10, 1), new Employee { Id = 50, BranchId = 2 });

        var dto = new CreateEmployeeDto { BranchId = 2, Name = "Empleado Prueba" };
        await Assert.ThrowsAsync<ForbiddenException>(() => service.CreateAsync(dto, 10));
        employees.Verify(x => x.AddAsync(It.IsAny<Employee>()), Times.Never);
    }

    [Fact]
    public async Task AdminCannotDeactivateEmployeeInAnotherBranch()
    {
        var (service, employees, _, _) = BuildService(
            Admin(10, 1), new Employee { Id = 50, BranchId = 2, Status = "Activo" });

        await Assert.ThrowsAsync<ForbiddenException>(() => service.DeactivateAsync(50, 10));
        employees.Verify(x => x.UpdateAsync(It.IsAny<Employee>()), Times.Never);
    }

    [Fact]
    public void CustomerMeRequiresAuthenticationAndClientRole()
    {
        var controller = typeof(CustomersController);
        Assert.NotNull(controller.GetCustomAttribute<AuthorizeAttribute>());
        Assert.NotNull(controller.GetMethod(nameof(CustomersController.Register))!
            .GetCustomAttribute<AllowAnonymousAttribute>());

        foreach (var name in new[] {
            nameof(CustomersController.GetMe), nameof(CustomersController.UpdateMe) })
        {
            var attribute = controller.GetMethod(name)!
                .GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(attribute);
            Assert.Equal(RoleNames.Cliente, attribute!.Roles);
        }
    }

    private static User Admin(int userId, int? branchId) => new()
    {
        Id = userId,
        Employee = new Employee { Id = 100 + userId, BranchId = branchId },
        UserRoles = [new UserRole { Role = new Role { Name = RoleNames.Administrador } }]
    };

    private static (
        EmployeeService Service,
        Mock<IEmployeeRepository> Employees,
        Mock<IAuthRepository> Auth,
        Mock<ProjectAuthorizationService> Permissions) BuildService(User actor, Employee employee)
    {
    var employees = new Mock<IEmployeeRepository>();
    var auth = new Mock<IAuthRepository>();
    var permissions = new Mock<ProjectAuthorizationService>();

        auth.Setup(x => x.GetByIdWithRelationsAsync(actor.Id)).ReturnsAsync(actor);
        employees.Setup(x => x.GetByIdAsync(employee.Id)).ReturnsAsync(employee);
        permissions.Setup(x => x.CanPerformActionAsync(It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var service = new EmployeeService(
            employees.Object,
            null!, // Validator; no se invoca en estos caminos de rechazo/lectura.
            auth.Object,
            null!, // NotificationService
            permissions.Object,
            null!, // EmployeeActivationService
            null!, // RefreshTokenRepository
            null!, // DbContext
            null!, // DeliveryAssignmentService
            NullLogger<EmployeeService>.Instance,
            null!); // IRealtimeNotifier

        return (service, employees, auth, permissions);
    }
}
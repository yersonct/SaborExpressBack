using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SaborExpress.Configuration;
using SaborExpress.Controllers;
using SaborExpress.Data;
using SaborExpress.Modules.Auth.DTOs;
using SaborExpress.Modules.Auth.Helpers;
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Auth.Models;
using SaborExpress.Modules.Customers.DTOs;
using SaborExpress.Modules.Customers.Interfaces;
using SaborExpress.Modules.Customers.Models;
using SaborExpress.Modules.EmployeeSchedules.Interfaces;
using SaborExpress.Modules.EmployeeSchedules.Enum;
using SaborExpress.Modules.EmployeeSchedules.Models;
using SaborExpress.Modules.Employees.Interfaces;
using SaborExpress.Modules.Employees.Models;
using ActualEmployeeService = SaborExpress.Modules.Employees.Services.EmployeeService;
using SaborExpress.Modules.Notifications.Interfaces;
using SaborExpress.Modules.RolePermissions.DTOs;
using SaborExpress.Modules.RolePermissions.Interfaces;
using SaborExpress.Modules.Roles.Models;
using SaborExpress.Modules.UsersRoles.Models;
using SaborExpress.Shared.Constants;
using SaborExpress.Shared.Filters;
using SaborExpress.Shared.Interfaces;
using SaborExpress.Shared.Responses;
using Xunit;

namespace SaborExpressBack.Security.Tests;

// Actual controllers, AuthService, JWT validation and authorization run over HTTP.
// Repositories are isolated in-memory doubles: no live database, email or payments.
public sealed class HttpAuthenticationTests
{
    [Fact]
    public async Task LoginRefreshAndRevocationAreEnforcedOverHttp()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.Server.CreateClient();
        var login = await client.PostAsJsonAsync("/api/Auth/login", new LoginDto { Identifier = "yersonct", Password = fixture.Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var session = await login.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.False(string.IsNullOrWhiteSpace(session!.Token));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/Customers/me")).StatusCode);
        var refresh = await client.PostAsJsonAsync("/api/Auth/refresh-token", new RefreshTokenRequestDto { RefreshToken = session.RefreshToken });
        Assert.True(refresh.StatusCode == HttpStatusCode.OK, await refresh.Content.ReadAsStringAsync());
        var replay = await client.PostAsJsonAsync("/api/Auth/refresh-token", new RefreshTokenRequestDto { RefreshToken = session.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
    }

    [Fact]
    public async Task FailedLoginsLockTheAccountAndReturnRetryAfter()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.Server.CreateClient();
        for (var attempt = 0; attempt < 5; attempt++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/Auth/login", new LoginDto { Identifier = "yersonct", Password = "invalid-test-password" })).StatusCode);
        var blocked = await client.PostAsJsonAsync("/api/Auth/login", new LoginDto { Identifier = "yersonct", Password = fixture.Password });
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.True(blocked.Headers.Contains("Retry-After"));
    }

    [Fact]
    public async Task MissingTokenWrongRoleAndOtherBranchAreRejected()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.Server.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/Customers/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/Customers/me")).StatusCode);
        var token = fixture.TokenFor(fixture.Customer);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var denied = await client.GetAsync("/api/Employees/50/cv");
        Assert.True(denied.StatusCode == HttpStatusCode.Forbidden, await denied.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fixture.TokenFor(fixture.Admin));
        var crossBranch = await client.GetAsync("/api/Employees/50/cv");
        Assert.Equal(HttpStatusCode.Forbidden, crossBranch.StatusCode);
        var crossBranchError = await crossBranch.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(crossBranchError);
        Assert.Equal("FORBIDDEN", crossBranchError.Code);
        Assert.False(string.IsNullOrWhiteSpace(crossBranchError.TraceId));
    }

    // HU-SE-002: camino HTTP con controlador + servicio de empleados REAL.
    [Fact]
    public async Task CvHttpRequiresLoginEnforcesBranchAndDownloadsOwnBranchWithRealService()
    {
        using var fixture = new ApiFixture(useRealEmployeeService: true);
        using var client = fixture.Server.CreateClient();

        var anonymous = await client.GetAsync("/api/Employees/52/cv");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", fixture.TokenFor(fixture.Admin));
        var denied = await client.GetAsync("/api/Employees/50/cv");
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        var deniedBody = await denied.Content.ReadAsStringAsync();
        Assert.DoesNotContain("PRIVATE_BRANCH_TWO_CV", deniedBody);
        var deniedError = await denied.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(deniedError);
        Assert.Equal("FORBIDDEN", deniedError.Code);

        var authorized = await client.GetAsync("/api/Employees/52/cv");
        Assert.Equal(HttpStatusCode.OK, authorized.StatusCode);
        Assert.Equal("application/pdf", authorized.Content.Headers.ContentType?.MediaType);
        Assert.Equal(fixture.ValidCvPdf, await authorized.Content.ReadAsByteArrayAsync());
    }

    // HU-SE-003: GET/PUT me deben respetar identidad y rol a nivel HTTP.
    [Fact]
    public async Task CustomerMeHttpEnforcesAnonymousWrongRoleAndOwnProfileForGetAndPut()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.Server.CreateClient();
        var update = new UpdateCustomerDto
        {
            Name = "Maria", LastName = "Prueba",
            Phone = "3001234567", Address = "Calle 12 numero 34"
        };

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync("/api/v1/Customers/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PutAsJsonAsync("/api/v1/Customers/me", update)).StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", fixture.TokenFor(fixture.Admin));
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.GetAsync("/api/v1/Customers/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PutAsJsonAsync("/api/v1/Customers/me", update)).StatusCode);
        fixture.Customers.Verify(x => x.UpdateAsync(It.IsAny<Customer>()), Times.Never);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", fixture.TokenFor(fixture.Customer));
        var ownGet = await client.GetAsync("/api/v1/Customers/me");
        Assert.Equal(HttpStatusCode.OK, ownGet.StatusCode);
        var put = await client.PutAsJsonAsync("/api/v1/Customers/me", update);
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        var response = await put.Content.ReadFromJsonAsync<SaborExpress.Modules.Customers.DTOs.CustomerResponseDto>();
        Assert.NotNull(response);
        Assert.Equal(fixture.Customer.Id, response.UserId);
        Assert.Equal("Maria", response.Name);
        fixture.Customers.Verify(x => x.UpdateAsync(It.Is<Customer>(c =>
            c.UserId == fixture.Customer.Id && c.Name == "Maria")), Times.Once);
        fixture.Customers.Verify(x => x.UpdateAsync(It.Is<Customer>(c =>
            c.UserId != fixture.Customer.Id)), Times.Never);
    }

    [Fact]
    public async Task GlobalRolePermissionMutationsRequireManagerRole()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.Server.CreateClient();
        var permission = new CreateRolePermissionDto { RoleId = 5, PermissionId = 9 };

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/api/role-permissions", permission)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.DeleteAsync("/api/role-permissions/5/9")).StatusCode);

        foreach (var role in new[]
        {
            RoleNames.Administrador, RoleNames.Mesero, RoleNames.Cajero,
            RoleNames.Cocinero, RoleNames.AuxiliarCocina,
            RoleNames.Repartidor, RoleNames.Cliente
        })
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", fixture.TokenForRole(role));
            Assert.Equal(HttpStatusCode.Forbidden,
                (await client.PostAsJsonAsync("/api/role-permissions", permission)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden,
                (await client.DeleteAsync("/api/role-permissions/5/9")).StatusCode);
        }

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", fixture.TokenForRole(RoleNames.Gerente));
        Assert.Equal(HttpStatusCode.Created,
            (await client.PostAsJsonAsync("/api/role-permissions", permission)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.DeleteAsync("/api/role-permissions/5/9")).StatusCode);

        fixture.RolePermissions.Verify(service => service.AssignAsync(
            It.Is<CreateRolePermissionDto>(dto => dto.RoleId == 5 && dto.PermissionId == 9),
            It.IsAny<ClaimsPrincipal>()), Times.Once);
        fixture.RolePermissions.Verify(service => service.RemoveAsync(
            5, 9, It.IsAny<ClaimsPrincipal>()), Times.Once);
    }

    [Fact]
    public async Task CvDownloadUsesStandardErrorContractAndPreservesValidFileResponseOverHttp()
    {
        using var fixture = new ApiFixture();
        using var client = fixture.Server.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", fixture.TokenForRole(RoleNames.Gerente));

        var missing = await client.GetAsync("/api/Employees/51/cv");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        var error = await missing.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Equal("NOT_FOUND", error.Code);
        Assert.Equal("Este empleado no tiene CV cargado", error.Message);
        Assert.False(string.IsNullOrWhiteSpace(error.TraceId));

        var pdf = "%PDF-1.7\n%%EOF"u8.ToArray();
        fixture.EmployeeService
            .Setup(service => service.GetCvAsync(51, It.IsAny<int>()))
            .ReturnsAsync((pdf, "hoja-de-vida.pdf", "application/pdf"));

        var download = await client.GetAsync("/api/Employees/51/cv");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("application/pdf", download.Content.Headers.ContentType?.MediaType);
        Assert.Equal(pdf, await download.Content.ReadAsByteArrayAsync());
        Assert.Contains("hoja-de-vida.pdf", download.Content.Headers.ContentDisposition?.FileName);
    }

    private sealed class ApiFixture : IDisposable
    {
        public TestServer Server { get; }
        public string Password { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
        public User Customer { get; }
        public User Admin { get; }
        public Mock<IRolePermissionService> RolePermissions { get; } = new();
        public Mock<IEmployeeService> EmployeeService { get; } = new();
        public Mock<ICustomerRepository> Customers { get; } = new();
        public byte[] ValidCvPdf { get; } = Convert.FromBase64String("JVBERi0xLjMKJZOMi54gUmVwb3J0TGFiIEdlbmVyYXRlZCBQREYgZG9jdW1lbnQgKG9wZW5zb3VyY2UpCjEgMCBvYmoKPDwKL0YxIDIgMCBSCj4+CmVuZG9iagoyIDAgb2JqCjw8Ci9CYXNlRm9udCAvSGVsdmV0aWNhIC9FbmNvZGluZyAvV2luQW5zaUVuY29kaW5nIC9OYW1lIC9GMSAvU3VidHlwZSAvVHlwZTEgL1R5cGUgL0ZvbnQKPj4KZW5kb2JqCjMgMCBvYmoKPDwKL0NvbnRlbnRzIDcgMCBSIC9NZWRpYUJveCBbIDAgMCAyMDAgMjAwIF0gL1BhcmVudCA2IDAgUiAvUmVzb3VyY2VzIDw8Ci9Gb250IDEgMCBSIC9Qcm9jU2V0IFsgL1BERiAvVGV4dCAvSW1hZ2VCIC9JbWFnZUMgL0ltYWdlSSBdCj4+IC9Sb3RhdGUgMCAvVHJhbnMgPDwKCj4+IAogIC9UeXBlIC9QYWdlCj4+CmVuZG9iago0IDAgb2JqCjw8Ci9QYWdlTW9kZSAvVXNlTm9uZSAvUGFnZXMgNiAwIFIgL1R5cGUgL0NhdGFsb2cKPj4KZW5kb2JqCjUgMCBvYmoKPDwKL0F1dGhvciAoYW5vbnltb3VzKSAvQ3JlYXRpb25EYXRlIChEOjIwMjYxMTA1MjE0MzQ1KzAwJzAwJykgL0NyZWF0b3IgKGFub255bW91cykgL0tleXdvcmRzICgpIC9Nb2REYXRlIChEOjIwMjYxMTA1MjE0MzQ1KzAwJzAwJykgL1Byb2R1Y2VyIChSZXBvcnRMYWIgUERGIExpYnJhcnkgLSBcKG9wZW5zb3VyY2VcKSkgCiAgL1N1YmplY3QgKHVuc3BlY2lmaWVkKSAvVGl0bGUgKHVudGl0bGVkKSAvVHJhcHBlZCAvRmFsc2UKPj4KZW5kb2JqCjYgMCBvYmoKPDwKL0NvdW50IDEgL0tpZHMgWyAzIDAgUiBdIC9UeXBlIC9QYWdlcwo+PgplbmRvYmoKNyAwIG9iago8PAovRmlsdGVyIFsgL0FTQ0lJODVEZWNvZGUgL0ZsYXRlRGVjb2RlIF0gL0xlbmd0aCAxMTIKPj4Kc3RyZWFtCkdhcFFoMEU9RiwwVVxIM1RccE5ZVF5RS2s/dGM+SVAsO1cjVTFeMjNpaFBFTV8/Q1c0S0lTaFwmZEFPSStoN3B1Yz0lJi9XPHJldFY1WyVDJEckU2BXQmQmTVZGIWg+NkFRQ0pRTkBzJ2VBO3I6fj5lbmRzdHJlYW0KZW5kb2JqCnhyZWYKMCA4CjAwMDAwMDAwMDAgNjU1MzUgZiAKMDAwMDAwMDA2MSAwMDAwMCBuIAowMDAwMDAwMDkyIDAwMDAwIG4gCjAwMDAwMDAxOTkgMDAwMDAgbiAKMDAwMDAwMDM5MiAwMDAwMCBuIAowMDAwMDAwNDYwIDAwMDAwIG4gCjAwMDAwMDA3MjEgMDAwMDAgbiAKMDAwMDAwMDc4MCAwMDAwMCBuIAp0cmFpbGVyCjw8Ci9JRCAKWzxmZmZhM2E5MjBiM2IxOWE3NDk2ZGE3NjQ1YjRlOGU4Nz48ZmZmYTNhOTIwYjNiMTlhNzQ5NmRhNzY0NWI0ZThlODc+XQolIFJlcG9ydExhYiBnZW5lcmF0ZWQgUERGIGRvY3VtZW50IC0tIGRpZ2VzdCAob3BlbnNvdXJjZSkKCi9JbmZvIDUgMCBSCi9Sb290IDQgMCBSCi9TaXplIDgKPj4Kc3RhcnR4cmVmCjk4MgolJUVPRgo=");
        private readonly Mock<IEmployeeScheduleRepository> _schedules = new();
        private readonly JwtSettings _settings;
        public ApiFixture(bool useRealEmployeeService = false)
        {
            Customer = new User { Id = 101, Status = true, EmailConfirmed = true, Email = "yersonct@example.invalid",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password, 4),
                Customer = new Customer { Id = 201, UserId = 101, Name = "yersonct" },
                UserRoles = [new UserRole { Role = new Role { Name = RoleNames.Cliente } }] };
            Admin = new User { Id = 102, Status = true, EmailConfirmed = true,
                Employee = new Employee { Id = 202, BranchId = 1 },
                UserRoles = [new UserRole { Role = new Role { Name = RoleNames.Administrador } }] };
            _settings = new JwtSettings { SecretKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)),
                Issuer = "http-security-tests", Audience = "http-security-tests", ExpirationInMinutes = 30, RefreshTokenExpirationHours = 1 };
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = _settings.SecretKey,
                ["JwtSettings:Issuer"] = _settings.Issuer,
                ["JwtSettings:Audience"] = _settings.Audience,
                ["JwtSettings:ExpirationInMinutes"] = "30",
                ["JwtSettings:RefreshTokenExpirationHours"] = "1",
                ["Auth:SingleSession"] = "true"
            }).Build();
            var users = new Dictionary<int, User> { [101] = Customer, [102] = Admin };
            var auth = new Mock<IAuthRepository>();
            auth.Setup(r => r.GetByIdentifierAsync(It.IsAny<string>())).ReturnsAsync((string identifier) => identifier == "yersonct" ? Customer : null);
            auth.Setup(r => r.GetByIdWithRelationsAsync(It.IsAny<int>())).ReturnsAsync((int id) => users.GetValueOrDefault(id));
            auth.Setup(r => r.GetSessionInfoAsync(It.IsAny<int>())).ReturnsAsync((int id) => (users.ContainsKey(id), users.GetValueOrDefault(id)?.Token));
            var refreshTokens = new Dictionary<string, RefreshToken>();
            var refresh = new Mock<IRefreshTokenRepository>();
            refresh.Setup(r => r.AddAsync(It.IsAny<RefreshToken>())).Callback<RefreshToken>(token => refreshTokens[token.Token] = token).Returns(Task.CompletedTask);
            refresh.Setup(r => r.GetActiveByTokenAsync(It.IsAny<string>())).ReturnsAsync((string token) => refreshTokens.TryGetValue(token, out var stored) && !stored.IsRevoked && stored.ExpiresAt > DateTime.UtcNow ? stored : null);
            refresh.Setup(r => r.RevokeAsync(It.IsAny<RefreshToken>())).Callback<RefreshToken>(token => token.IsRevoked = true).Returns(Task.CompletedTask);
            refresh.Setup(r => r.RevokeAllForUserAsync(It.IsAny<int>())).Callback<int>(id => { foreach (var token in refreshTokens.Values.Where(t => t.UserId == id)) token.IsRevoked = true; }).Returns(Task.CompletedTask);
            _schedules.Setup(r => r.CanActAsAdministratorAsync(102, It.IsAny<DateTime>())).ReturnsAsync(true);
            _schedules.Setup(r => r.GetByEmployeeAndDateAsync(202, It.IsAny<DateOnly>()))
                .ReturnsAsync((int _, DateOnly date) =>
                [new EmployeeSchedule
                {
                    EmployeeId = 202,
                    BranchId = 1,
                    RoleId = Admin.UserRoles.Single().RoleId,
                    ShiftDate = date,
                    StartTime = TimeOnly.MinValue,
                    EndTime = TimeOnly.MaxValue,
                    Status = ScheduleStatus.Programado
                }]);
            var employees = new Mock<IEmployeeRepository>();
            employees.Setup(r => r.GetByIdAsync(50)).ReturnsAsync(new Employee { Id = 50, BranchId = 2, CvFile = "%PDF-1.7 PRIVATE_BRANCH_TWO_CV"u8.ToArray() });
            employees.Setup(r => r.GetByIdAsync(52)).ReturnsAsync(new Employee { Id = 52, BranchId = 1, CvFile = ValidCvPdf, CvFilename = "cv-test.pdf", CvContentType = "application/pdf" });
            EmployeeService.Setup(service => service.GetCvAsync(50, 102))
                .ThrowsAsync(new SaborExpress.Shared.Exceptions.ForbiddenException("No puedes consultar el CV de otra sede."));
            EmployeeService.Setup(service => service.GetCvAsync(51, 102))
                .ReturnsAsync((ValueTuple<byte[], string, string>?)null);
            Customers.Setup(r => r.GetByUserIdAsync(101)).ReturnsAsync(Customer.Customer);
            Customers.Setup(r => r.UpdateAsync(It.IsAny<Customer>())).Returns(Task.CompletedTask);
            RolePermissions.Setup(service => service.AssignAsync(
                    It.IsAny<CreateRolePermissionDto>(), It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(new RolePermissionResponseDto
                {
                    RoleId = 5,
                    RoleName = "MESERO",
                    PermissionId = 9,
                    PermissionName = "VER_PEDIDOS",
                    AssignedAt = DateTime.UtcNow
                });
            RolePermissions.Setup(service => service.RemoveAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<ClaimsPrincipal>()))
                .Returns(Task.CompletedTask);
            Server = new TestServer(new WebHostBuilder().UseEnvironment("Development")
                .ConfigureAppConfiguration((_, c) => c.AddConfiguration(configuration))
                .ConfigureServices(services =>
                {
                    services.AddLogging(); services.AddMemoryCache(); services.AddHttpClient(); services.AddSignalR();
                    services.AddDbContext<AppDbContext>(options => options.UseNpgsql("Host=localhost;Database=isolated_test;Username=test"));
                    services.AddDependencies();
                    services.AddSingleton<IAuthTransactionRunner, ImmediateAuthTransactionRunner>();
                    services.AddSingleton<IConfiguration>(configuration);
                    if (useRealEmployeeService)
                    {
                        services.AddSingleton<IEmployeeService>(new ActualEmployeeService(
                            employees.Object, null!, auth.Object, null!, null!, null!, null!,
                            null!, null!, NullLogger<ActualEmployeeService>.Instance, null!));
                    }
                    else
                    {
                        services.AddSingleton(EmployeeService.Object);
                    }
                    services.AddSingleton<SaborExpress.Shared.Localization.IApiMessageLocalizer,
                        SaborExpress.Shared.Localization.ApiMessageLocalizer>();
                    services.AddSingleton(auth.Object); services.AddSingleton(refresh.Object);
                    services.AddSingleton(_schedules.Object); services.AddSingleton(employees.Object); services.AddSingleton(Customers.Object);
                    services.AddSingleton(Mock.Of<INotificationService>());
                    services.AddSingleton(Mock.Of<SaborExpress.Modules.Deliveries.Interfaces.IDeliveryAssignmentService>());
                    services.AddSingleton(Mock.Of<IEmployeeActivationService>());
                    services.AddSingleton(Mock.Of<SaborExpress.Shared.Interfaces.IAuthorizationService>());
                    services.AddSingleton(Mock.Of<INotificationRepository>());
                    services.AddSingleton(Mock.Of<IRealtimeNotifier>());
                    services.AddSingleton(RolePermissions.Object);
                    services.AddJwtConfiguration(configuration);
                    services.AddAuthorization(options => options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
                    services.AddControllers(options => { options.Conventions.Add(new ApiVersionConvention()); options.Filters.Add<ApiPaginationFilter>(); options.Filters.Add<ApiExceptionFilter>(); options.Filters.Add<ApiErrorResultFilter>(); })
                        .AddApplicationPart(typeof(AuthController).Assembly);
                })
                .Configure(app => { app.UseRouting(); app.UseAuthentication(); app.UseAuthorization(); app.UseEndpoints(endpoints => endpoints.MapControllers()); }));
        }
        public string TokenFor(User user)
        {
            var generator = new JwtTokenGenerator(Microsoft.Extensions.Options.Options.Create(_settings));
            var token = generator.Generate(user, "yersonct", effectiveRoles: user.UserRoles.Select(r => r.Role.Name).ToArray());
            user.Token = token;
            return token;
        }
        public string TokenForRole(string role)
        {
            var user = role == RoleNames.Cliente ? Customer : Admin;
            var roleId = role switch
            {
                RoleNames.Gerente => 1,
                RoleNames.Administrador => 2,
                RoleNames.Cliente => 3,
                RoleNames.Mesero => 4,
                RoleNames.Cajero => 5,
                RoleNames.Cocinero => 6,
                RoleNames.AuxiliarCocina => 7,
                RoleNames.Repartidor => 8,
                _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Rol de prueba no reconocido.")
            };
            user.UserRoles = [new UserRole
            {
                UserId = user.Id,
                RoleId = roleId,
                Role = new Role { Id = roleId, Name = role }
            }];
            var generator = new JwtTokenGenerator(Microsoft.Extensions.Options.Options.Create(_settings));
            var token = generator.Generate(user, "yersonct", effectiveRoles: [role]);
            user.Token = token;
            return token;
        }
        public void Dispose() => Server.Dispose();

        private sealed class ImmediateAuthTransactionRunner : IAuthTransactionRunner
        {
            public Task<T> ExecuteSerializableAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return operation();
            }
        }
    }
}

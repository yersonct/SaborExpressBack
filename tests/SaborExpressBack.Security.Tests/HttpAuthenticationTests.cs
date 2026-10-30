using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
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
using SaborExpress.Modules.Customers.Interfaces;
using SaborExpress.Modules.Customers.Models;
using SaborExpress.Modules.EmployeeSchedules.Interfaces;
using SaborExpress.Modules.Employees.Interfaces;
using SaborExpress.Modules.Employees.Models;
using SaborExpress.Modules.Notifications.Interfaces;
using SaborExpress.Modules.Roles.Models;
using SaborExpress.Modules.UsersRoles.Models;
using SaborExpress.Shared.Constants;
using SaborExpress.Shared.Filters;
using SaborExpress.Shared.Interfaces;
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
        Assert.True(crossBranch.StatusCode == HttpStatusCode.Forbidden, await crossBranch.Content.ReadAsStringAsync());
    }

    private sealed class ApiFixture : IDisposable
    {
        public TestServer Server { get; }
        public string Password { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
        public User Customer { get; }
        public User Admin { get; }
        private readonly JwtSettings _settings;
        public ApiFixture()
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
            var schedules = new Mock<IEmployeeScheduleRepository>();
            schedules.Setup(r => r.CanActAsAdministratorAsync(102, It.IsAny<DateTime>())).ReturnsAsync(true);
            var employees = new Mock<IEmployeeRepository>();
            employees.Setup(r => r.GetByIdAsync(50)).ReturnsAsync(new Employee { Id = 50, BranchId = 2, CvFile = "%PDF-1.7"u8.ToArray() });
            var customers = new Mock<ICustomerRepository>();
            customers.Setup(r => r.GetByUserIdAsync(101)).ReturnsAsync(Customer.Customer);
            Server = new TestServer(new WebHostBuilder().UseEnvironment("Development")
                .ConfigureAppConfiguration((_, c) => c.AddConfiguration(configuration))
                .ConfigureServices(services =>
                {
                    services.AddLogging(); services.AddMemoryCache(); services.AddHttpClient(); services.AddSignalR();
                    services.AddDbContext<AppDbContext>(options => options.UseNpgsql("Host=localhost;Database=isolated_test;Username=test"));
                    services.AddDependencies();
                    services.AddSingleton<IConfiguration>(configuration);
                    services.AddSingleton(auth.Object); services.AddSingleton(refresh.Object);
                    services.AddSingleton(schedules.Object); services.AddSingleton(employees.Object); services.AddSingleton(customers.Object);
                    services.AddSingleton(Mock.Of<INotificationService>());
                    services.AddSingleton(Mock.Of<SaborExpress.Modules.Deliveries.Interfaces.IDeliveryAssignmentService>());
                    services.AddSingleton(Mock.Of<IEmployeeActivationService>());
                    services.AddSingleton(Mock.Of<SaborExpress.Shared.Interfaces.IAuthorizationService>());
                    services.AddSingleton(Mock.Of<INotificationRepository>());
                    services.AddSingleton(Mock.Of<IRealtimeNotifier>());
                    services.AddJwtConfiguration(configuration);
                    services.AddAuthorization(options => options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
                    services.AddControllers(options => { options.Conventions.Add(new ApiVersionConvention()); options.Filters.Add<ApiPaginationFilter>(); options.Filters.Add<ApiExceptionFilter>(); })
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
        public void Dispose() => Server.Dispose();
    }
}

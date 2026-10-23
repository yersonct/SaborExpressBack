using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using SaborExpress.Configuration;
using SaborExpress.Controllers;
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Auth.Models;
using SaborExpress.Modules.Customers.Interfaces;
using SaborExpress.Modules.Deliveries.Interfaces;
using SaborExpress.Modules.Employees.Interfaces;
using SaborExpress.Modules.Employees.Models;
using SaborExpress.Modules.Orders.Interfaces;
using SaborExpress.Modules.Orders.Models;
using SaborExpress.Modules.Payments.DTOs;
using SaborExpress.Modules.Payments.Interfaces;
using SaborExpress.Modules.Payments.Services;
using SaborExpress.Modules.Payments.Validators;
using SaborExpress.Modules.Tables.Interfaces;
using SaborExpress.Modules.Notifications.Interfaces;
using SaborExpress.Shared.Constants;
using SaborExpress.Shared.Filters;
using SaborExpress.Shared.Exceptions;
using SaborExpress.Shared.Interfaces;
using SaborExpress.Shared.Security;
using SaborExpress.Shared.Services;
using Xunit;

namespace SaborExpressBack.Security.Tests;

public sealed class SecurityRegressionTests
{
    [Fact]
    public void ProductionCorsAllowsOnlyExactConfiguredPublicOrigins()
    {
        var configured = CorsOriginPolicy.FilterConfiguredOrigins(
        [
            "https://admin.example.test",
            "http://localhost:3000",
            "https://preview.devtunnels.ms",
            "https://admin.example.test/path"
        ],
        isDevelopment: false);

        Assert.True(CorsOriginPolicy.IsAllowedOrigin("https://admin.example.test", configured, false));
        Assert.False(CorsOriginPolicy.IsAllowedOrigin("https://admin.example.test.attacker.invalid", configured, false));
        Assert.False(CorsOriginPolicy.IsAllowedOrigin("http://localhost:3000", configured, false));
        Assert.False(CorsOriginPolicy.IsAllowedOrigin("https://preview.devtunnels.ms", configured, false));
        Assert.DoesNotContain("https://admin.example.test/path", configured);
    }

    [Fact]
    public void EmployeeCannotReadOrdersFromAnotherBranch()
    {
        var identity = Principal(RoleNames.Cajero);
        var actor = new User { Employee = new Employee { Id = 12, BranchId = 3 } };
        var order = new Order { Id = 81, BranchId = 9 };

        Assert.False(ResourceReadPolicy.CanReadBranch(identity, actor, order.BranchId));
        Assert.False(ResourceReadPolicy.CanReadOrder(identity, actor, order));
    }

    [Fact]
    public void ForbiddenAccessIsReturnedAsHttp403()
    {
        var exceptionContext = new ExceptionContext(
            new ActionContext(new DefaultHttpContext(), new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()),
            new List<IFilterMetadata>())
        {
            Exception = new ForbiddenException("Acceso denegado en prueba.")
        };

        new ApiExceptionFilter().OnException(exceptionContext);

        Assert.Equal(StatusCodes.Status403Forbidden, (exceptionContext.Result as ObjectResult)?.StatusCode);
        Assert.True(exceptionContext.ExceptionHandled);
    }

    [Fact]
    public void UnprivilegedKitchenRoleCannotReadFinancialDetails()
    {
        var identity = Principal(RoleNames.Cocinero);
        var actor = new User { Employee = new Employee { Id = 12, BranchId = 3 } };
        var order = new Order { Id = 81, BranchId = 3 };

        Assert.False(ResourceReadPolicy.CanReadPaymentOfOrder(identity, actor, order));
    }

    [Fact]
    public void ManagerCanReadOrdersAcrossBranches()
    {
        var identity = Principal(RoleNames.Gerente);
        var actor = new User { Employee = new Employee { Id = 12, BranchId = 3 } };
        var order = new Order { Id = 81, BranchId = 9 };

        Assert.True(ResourceReadPolicy.CanReadOrder(identity, actor, order));
    }

    [Fact]
    public void ExpiredJwtIsRejectedWithoutClockSkew()
    {
        var values = new Dictionary<string, string?>
        {
            ["JwtSettings:SecretKey"] = new string('k', 48),
            ["JwtSettings:Issuer"] = "security-regression-tests",
            ["JwtSettings:Audience"] = "security-regression-tests"
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddJwtConfiguration(configuration);

        using var provider = services.BuildServiceProvider();
        var validation = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme).TokenValidationParameters;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(values["JwtSettings:SecretKey"]!));
        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: "security-regression-tests",
            audience: "security-regression-tests",
            claims: [new Claim(ClaimTypes.NameIdentifier, "12")],
            notBefore: now.AddMinutes(-5),
            expires: now.AddMinutes(-1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        Assert.Equal(TimeSpan.Zero, validation.ClockSkew);
        Assert.Throws<SecurityTokenExpiredException>(() =>
            new JwtSecurityTokenHandler().ValidateToken(
                new JwtSecurityTokenHandler().WriteToken(token), validation, out _));
    }

    [Fact]
    public async Task InvalidWompiSignatureReturnsUnauthorizedAndDoesNotLogPayloadOrChecksum()
    {
        const string rawBody = "synthetic-webhook-body-marker";
        const string signature = "synthetic-checksum-marker";
        var wompi = new Mock<IWompiClient>();
        wompi.Setup(client => client.VerifyWebhookSignature(rawBody, signature)).Returns(false);
        var logger = new CapturingLogger<PaymentService>();
        var service = CreatePaymentService(wompi.Object, logger);

        var error = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ProcessWompiWebhookAsync(new WompiWebhookDto(), rawBody, signature));

        var exceptionContext = new ExceptionContext(
            new ActionContext(new DefaultHttpContext(), new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()),
            new List<IFilterMetadata>())
        {
            Exception = error
        };
        new ApiExceptionFilter().OnException(exceptionContext);

        Assert.Equal(StatusCodes.Status401Unauthorized, (exceptionContext.Result as ObjectResult)?.StatusCode);
        Assert.True(exceptionContext.ExceptionHandled);
        Assert.DoesNotContain(rawBody, string.Join(Environment.NewLine, logger.Messages));
        Assert.DoesNotContain(signature, string.Join(Environment.NewLine, logger.Messages));
        wompi.Verify(client => client.GetTransactionByReferenceAsync(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.BadRequest)]
    public async Task EmailLogsDoNotContainRecipientOrProviderResponse(HttpStatusCode statusCode)
    {
        const string recipient = "synthetic-recipient@example.invalid";
        const string providerDetail = "synthetic-provider-response-marker";
        var logger = new CapturingLogger<NotificationService>();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Brevo:ApiKey"] = new string('k', 48),
                ["EmailSettings:Username"] = "synthetic-sender@example.invalid"
            }).Build();
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(providerDetail)
        });
        var service = new NotificationService(logger, configuration, new StubHttpClientFactory(handler));

        if (statusCode == HttpStatusCode.OK)
            await service.SendEmailAsync(recipient, "Asunto de prueba", "Contenido sintético.");
        else
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.SendEmailAsync(recipient, "Asunto de prueba", "Contenido sintético."));

        var logged = string.Join(Environment.NewLine, logger.Messages);
        Assert.DoesNotContain(recipient, logged, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(providerDetail, logged, StringComparison.OrdinalIgnoreCase);
    }

    private static ClaimsPrincipal Principal(string role) => new(
        new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], authenticationType: "security-test"));

    private static PaymentService CreatePaymentService(IWompiClient wompi, ILogger<PaymentService> logger)
    {
        var payments = Mock.Of<IPaymentRepository>();
        var orders = Mock.Of<IOrderRepository>();
        return new PaymentService(
            payments,
            new PaymentValidator(payments, orders),
            Mock.Of<IAuthRepository>(),
            Mock.Of<IAuthorizationService>(),
            wompi,
            orders,
            Mock.Of<ITableAvailabilityService>(),
            Mock.Of<IDeliveryAssignmentService>(),
            Mock.Of<IUserNotificationService>(),
            Mock.Of<ICustomerRepository>(),
            Mock.Of<IEmployeeRepository>(),
            logger,
            Mock.Of<IRealtimeNotifier>());
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
            if (exception is not null)
                Messages.Add(exception.ToString());
        }
    }

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(responseFactory(request));
    }
}

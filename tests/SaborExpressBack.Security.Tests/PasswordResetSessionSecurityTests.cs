using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SaborExpress.Configuration;
using SaborExpress.Data;
using SaborExpress.Modules.Auth.DTOs;
using SaborExpress.Modules.Auth.Helpers;
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Auth.Models;
using SaborExpress.Modules.Auth.Services;
using SaborExpress.Modules.Auth.Validators;
using SaborExpress.Modules.Branches.Interfaces;
using SaborExpress.Modules.Customers.Interfaces;
using SaborExpress.Modules.EmployeeSchedules.Interfaces;
using SaborExpress.Modules.Notifications.Interfaces;
using SaborExpress.Modules.Roles.Models;
using SaborExpress.Modules.UsersRoles.Models;
using SaborExpress.Shared.Constants;
using SaborExpress.Shared.Interfaces;
using Xunit;

namespace SaborExpressBack.Security.Tests;

public sealed class PasswordResetSessionSecurityTests
{
    private const int UserId = 48;
    private const string ResetToken = "test-reset-token";
    private const string OldRefreshToken = "test-old-refresh-token";
    private const string OldPassword = "OldPassword123";
    private const string NewPassword = "NewPassword456";

    [Fact]
    public async Task ResetChangesPasswordInvalidatesAccessAndRevokesAllRefreshTokens()
    {
        var harness = CreateResetHarness();

        await harness.Service.ResetPasswordAsync(ResetRequest());

        Assert.True(BCrypt.Net.BCrypt.Verify(NewPassword, harness.User.PasswordHash));
        Assert.Null(harness.User.Token);
        Assert.True(harness.ResetCode.IsCompleted);
        Assert.True(harness.RefreshTokensRevoked);
        Assert.Equal(1, harness.TransactionRunner.CommittedCount);
        harness.RefreshRepository.Verify(repository => repository.RevokeAllForUserAsync(UserId), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenCreatedBeforePasswordRecoveryIsRejectedAfterRecovery()
    {
        var harness = CreateResetHarness();
        await harness.Service.ResetPasswordAsync(ResetRequest());
        var authService = CreateAuthService(harness);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => authService.RefreshTokenAsync(
            new RefreshTokenRequestDto { RefreshToken = OldRefreshToken }));
    }

    [Fact]
    public async Task OldAccessTokenIsRejectedByJwtSessionValidationAfterRecovery()
    {
        var harness = CreateResetHarness();
        var settings = JwtSettingsForTests();
        var oldAccessToken = new JwtTokenGenerator(Options.Create(settings))
            .Generate(harness.User, "manager@test.invalid", effectiveRoles: [RoleNames.Gerente]);
        harness.User.Token = oldAccessToken;
        await harness.Service.ResetPasswordAsync(ResetRequest());

        var configuration = JwtConfigurationForTests(singleSession: true);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IAuthRepository>(harness.AuthRepository.Object);
        JwtConfiguration.AddJwtConfiguration(services, configuration);
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        var context = new TokenValidatedContext(
            new DefaultHttpContext { RequestServices = provider },
            new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, "Bearer", typeof(JwtBearerHandler)),
            options)
        {
            Principal = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, UserId.ToString())], "Bearer")),
            SecurityToken = new JwtSecurityTokenHandler().ReadJwtToken(oldAccessToken)
        };
        harness.AuthRepository.Setup(repository => repository.GetSessionInfoAsync(UserId))
            .ReturnsAsync(() => (true, harness.User.Token));

        await options.Events.OnTokenValidated(context);

        Assert.NotNull(context.Result?.Failure);
    }

    [Fact]
    public async Task LoginAcceptsNewPasswordAndRejectsOldPasswordAfterRecovery()
    {
        var harness = CreateResetHarness();
        await harness.Service.ResetPasswordAsync(ResetRequest());
        var authService = CreateAuthService(harness);
        var login = new LoginDto { Identifier = "manager@test.invalid", Password = NewPassword };

        var response = await authService.LoginAsync(login);

        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.False(string.IsNullOrWhiteSpace(response.RefreshToken));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => authService.LoginAsync(
            new LoginDto { Identifier = login.Identifier, Password = OldPassword }));
    }

    [Fact]
    public async Task CompletedRecoveryTokenCannotBeReused()
    {
        var harness = CreateResetHarness();
        await harness.Service.ResetPasswordAsync(ResetRequest());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => harness.Service.ResetPasswordAsync(ResetRequest()));

        Assert.Equal(1, harness.TransactionRunner.CommittedCount);
        harness.RefreshRepository.Verify(repository => repository.RevokeAllForUserAsync(UserId), Times.Once);
    }

    [Fact]
    public async Task FailureRevokingRefreshTokensDoesNotCompleteRecoveryOrCommitTransaction()
    {
        var harness = CreateResetHarness();
        harness.RefreshRepository
            .Setup(repository => repository.RevokeAllForUserAsync(UserId))
            .ThrowsAsync(new InvalidOperationException("synthetic revoke failure"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => harness.Service.ResetPasswordAsync(ResetRequest()));

        Assert.False(harness.ResetCode.IsCompleted);
        Assert.Equal(0, harness.TransactionRunner.CommittedCount);
        harness.ResetRepository.Verify(repository => repository.UpdateAsync(It.IsAny<PasswordResetCode>()), Times.Never);
    }

    [Fact]
    public async Task ConcurrentRecoveryRequestsOnlyAllowOneUseOfResetToken()
    {
        var harness = CreateResetHarness();
        var attempts = await Task.WhenAll(
            CaptureResetAsync(harness.Service),
            CaptureResetAsync(harness.Service));

        Assert.Single(attempts, succeeded => succeeded);
        Assert.True(harness.ResetCode.IsCompleted);
        Assert.Equal(1, harness.TransactionRunner.CommittedCount);
        harness.RefreshRepository.Verify(repository => repository.RevokeAllForUserAsync(UserId), Times.Once);
    }

    [Fact]
    public async Task RefreshWaitingForRecoveryIsRejectedAfterRecoveryCommits()
    {
        var updateStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var continueRecovery = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var harness = CreateResetHarness(async _ =>
        {
            updateStarted.TrySetResult();
            await continueRecovery.Task;
        });
        var authService = CreateAuthService(harness);
        var resetTask = harness.Service.ResetPasswordAsync(ResetRequest());
        await updateStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var refreshTask = authService.RefreshTokenAsync(new RefreshTokenRequestDto { RefreshToken = OldRefreshToken });
        await Task.Delay(50);
        Assert.False(refreshTask.IsCompleted);

        continueRecovery.SetResult();
        await resetTask;
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => refreshTask);
    }

    private static async Task<bool> CaptureResetAsync(PasswordResetService service)
    {
        try
        {
            await service.ResetPasswordAsync(ResetRequest());
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static ResetHarness CreateResetHarness(Func<User, Task>? updateUser = null)
    {
        var user = new User
        {
            Id = UserId,
            Email = "manager@test.invalid",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(OldPassword),
            Token = "old-access-token",
            Status = true,
            EmailConfirmed = true,
            UserRoles =
            [
                new UserRole { UserId = UserId, RoleId = 1, Role = new Role { Id = 1, Name = RoleNames.Gerente } }
            ]
        };
        var resetCode = new PasswordResetCode
        {
            Id = 91,
            UserId = UserId,
            Code = "123456",
            ResetToken = ResetToken,
            ResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CodeExpiresAt = DateTime.UtcNow.AddMinutes(10),
            CreatedAt = DateTime.UtcNow
        };
        var authRepository = new Mock<IAuthRepository>();
        authRepository.Setup(repository => repository.GetByIdWithRelationsAsync(UserId)).ReturnsAsync(user);
        authRepository.Setup(repository => repository.GetByIdentifierAsync(user.Email!)).ReturnsAsync(user);
        authRepository.Setup(repository => repository.UpdateAsync(It.IsAny<User>()))
            .Returns((User updated) => updateUser?.Invoke(updated) ?? Task.CompletedTask);
        authRepository.Setup(repository => repository.GetSessionInfoAsync(UserId))
            .ReturnsAsync(() => (true, user.Token));

        var resetRepository = new Mock<IPasswordResetRepository>();
        resetRepository.Setup(repository => repository.GetByResetTokenAsync(ResetToken))
            .ReturnsAsync(() => resetCode.IsCompleted ? null : resetCode);
        resetRepository.Setup(repository => repository.UpdateAsync(It.IsAny<PasswordResetCode>()))
            .Returns(Task.CompletedTask);

        var refreshRepository = new Mock<IRefreshTokenRepository>();
        var wasRevoked = false;
        var oldRefresh = new RefreshToken
        {
            Id = 301,
            UserId = UserId,
            Token = OldRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        refreshRepository.Setup(repository => repository.GetActiveByTokenAsync(OldRefreshToken))
            .ReturnsAsync(() => wasRevoked ? null : oldRefresh);
        refreshRepository.Setup(repository => repository.RevokeAllForUserAsync(UserId))
            .Returns(() =>
            {
                wasRevoked = true;
                return Task.CompletedTask;
            });
        refreshRepository.Setup(repository => repository.AddAsync(It.IsAny<RefreshToken>()))
            .Returns(Task.CompletedTask);
        refreshRepository.Setup(repository => repository.RevokeAsync(It.IsAny<RefreshToken>()))
            .Returns(Task.CompletedTask);

        var transactionRunner = new InlineTransactionRunner();
        var userSessionLock = new UserSessionLock();
        var service = new PasswordResetService(
            authRepository.Object,
            resetRepository.Object,
            refreshRepository.Object,
            userSessionLock,
            transactionRunner,
            Mock.Of<INotificationService>(),
            Mock.Of<SaborExpress.Shared.Interfaces.IBackgroundEmailQueue>(),
            new ForgotPasswordValidator(),
            new VerifyResetCodeValidator(),
            new ResetPasswordValidator());

        return new ResetHarness(
            service, user, resetCode, authRepository, resetRepository, refreshRepository,
            transactionRunner, userSessionLock, () => wasRevoked);
    }

    private static AuthService CreateAuthService(ResetHarness harness)
    {
        var settings = JwtSettingsForTests();
        var configuration = JwtConfigurationForTests(singleSession: false);
        return new AuthService(
            harness.AuthRepository.Object,
            new LoginValidator(),
            new JwtTokenGenerator(Options.Create(settings)),
            Options.Create(settings),
            Mock.Of<INotificationService>(),
            Mock.Of<IRealtimeNotifier>(),
            harness.RefreshRepository.Object,
            Mock.Of<IEmployeeScheduleRepository>(),
            Mock.Of<ICustomerRepository>(),
            Mock.Of<SaborExpress.Modules.Notifications.Interfaces.INotificationRepository>(),
            configuration,
            Mock.Of<IBranchRepository>(),
            new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().Options),
            harness.UserSessionLock,
            harness.TransactionRunner);
    }

    private static ResetPasswordDto ResetRequest() => new()
    {
        ResetToken = ResetToken,
        NewPassword = NewPassword,
        ConfirmPassword = NewPassword
    };

    private static JwtSettings JwtSettingsForTests() => new()
    {
        SecretKey = "test-only-signing-key-with-more-than-32-bytes",
        Issuer = "SaborExpress.Security.Tests",
        Audience = "SaborExpress.Security.Tests",
        ExpirationInMinutes = 30,
        RefreshTokenExpirationHours = 12
    };

    private static IConfiguration JwtConfigurationForTests(bool singleSession) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JwtSettings:SecretKey"] = JwtSettingsForTests().SecretKey,
            ["JwtSettings:Issuer"] = JwtSettingsForTests().Issuer,
            ["JwtSettings:Audience"] = JwtSettingsForTests().Audience,
            ["Auth:SingleSession"] = singleSession.ToString()
        }).Build();

    private sealed class InlineTransactionRunner : IAuthTransactionRunner
    {
        public int CommittedCount { get; private set; }

        public async Task<T> ExecuteSerializableAsync<T>(
            Func<Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await operation();
            CommittedCount++;
            return result;
        }
    }

    private sealed record ResetHarness(
        PasswordResetService Service,
        User User,
        PasswordResetCode ResetCode,
        Mock<IAuthRepository> AuthRepository,
        Mock<IPasswordResetRepository> ResetRepository,
        Mock<IRefreshTokenRepository> RefreshRepository,
        InlineTransactionRunner TransactionRunner,
        IUserSessionLock UserSessionLock,
        Func<bool> WasRefreshTokensRevoked)
    {
        public bool RefreshTokensRevoked => WasRefreshTokensRevoked();
    }
}

#if HU01_POSTGRES_INTEGRATION
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Npgsql;
using SaborExpress.Configuration;
using SaborExpress.Data;
using SaborExpress.Modules.Auth.DTOs;
using SaborExpress.Modules.Auth.Helpers;
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Auth.Models;
using SaborExpress.Modules.Auth.Repositories;
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
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace SaborExpressBack.Security.Tests;

/// <summary>
/// Explicit opt-in integration coverage for a disposable, local PostgreSQL database.
/// Run with -p:HU01PostgresIntegration=true and HU01_PG_TEST_PASSWORD set from the
/// isolated local container. The target is fixed to 127.0.0.1:55432/hu01_test.
/// This test migrates and writes synthetic data.
/// </summary>
public sealed class PasswordResetPostgresIntegrationTests
{
    private const string OldPassword = "OldSyntheticPass-7!";
    private const string NewPassword = "NewSyntheticPass-8!";
    private const string ConcurrentPassword = "ConcurrentSyntheticPass-9!";

    [Fact]
    public async Task PasswordRecoveryRevokesSessionsRollsBackAndRetriesSerializableConflicts()
    {
        var password = Environment.GetEnvironmentVariable("HU01_PG_TEST_PASSWORD");
        Assert.False(string.IsNullOrWhiteSpace(password),
            "Set HU01_PG_TEST_PASSWORD from the isolated local container; no migration was run.");

        var connection = new NpgsqlConnectionStringBuilder
        {
            Host = "127.0.0.1",
            Port = 55432,
            Database = "hu01_test",
            Username = "hu01_test",
            Password = password,
            Pooling = false
        };
        var safeConnectionString = connection.ConnectionString;

        // Identity is checked over the live connection before any EF migration is allowed.
        await using (var preflight = new NpgsqlConnection(safeConnectionString))
        {
            await preflight.OpenAsync();
            await using var command = new NpgsqlCommand(
                "SELECT current_database(), current_user, current_setting('port')::integer",
                preflight);
            await using var reader = await command.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal("hu01_test", reader.GetString(0));
            Assert.Equal("hu01_test", reader.GetString(1));
            Assert.Equal(5432, reader.GetInt32(2));
        }

        var options = CreateOptions(safeConnectionString);
        await using (var migrationContext = new AppDbContext(options))
            await migrationContext.Database.MigrateAsync();

        var email = $"hu01-{Guid.NewGuid():N}@test.invalid";
        var managerRoleCreated = false;
        var userId = 0;

        try
        {
            await using (var setup = new AppDbContext(options))
            {
                var managerRole = await setup.Roles.SingleOrDefaultAsync(role => role.Name == RoleNames.Gerente);
                if (managerRole is null)
                {
                    managerRole = new Role
                    {
                        Name = RoleNames.Gerente,
                        Description = "HU-01 isolated integration test",
                        Status = true
                    };
                    setup.Roles.Add(managerRole);
                    await setup.SaveChangesAsync();
                    managerRoleCreated = true;
                }

                var user = new User
                {
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(OldPassword),
                    Status = true,
                    EmailConfirmed = true,
                    UserRoles =
                    [
                        new UserRole { RoleId = managerRole.Id, Role = managerRole }
                    ]
                };
                setup.Users.Add(user);
                await setup.SaveChangesAsync();
                userId = user.Id;
            }

            string originalAccessToken;
            string originalRefreshToken;
            string firstResetToken = Guid.NewGuid().ToString("N");
            await using (var context = new AppDbContext(options))
            {
                var auth = CreateAuthService(context);
                var initialLogin = await auth.LoginAsync(new LoginDto { Identifier = email, Password = OldPassword });
                originalAccessToken = initialLogin.Token;
                originalRefreshToken = initialLogin.RefreshToken;

                var authRepository = new AuthRepository(context);
                var beforeReset = await authRepository.GetSessionInfoAsync(userId);
                Assert.True(beforeReset.Exists);
                Assert.Equal(originalAccessToken, beforeReset.Token);

                context.PasswordResetCodes.Add(NewResetCode(userId, firstResetToken));
                await context.SaveChangesAsync();

                var reset = CreatePasswordResetService(context);
                await reset.ResetPasswordAsync(ResetRequest(firstResetToken, NewPassword));

                var afterReset = await authRepository.GetSessionInfoAsync(userId);
                Assert.True(afterReset.Exists);
                Assert.Null(afterReset.Token);
                await AssertAccessTokenRejectedAfterRecoveryAsync(context, userId, originalAccessToken);
                Assert.Null(await new RefreshTokenRepository(context).GetActiveByTokenAsync(originalRefreshToken));

                await Assert.ThrowsAsync<UnauthorizedAccessException>(() => auth.RefreshTokenAsync(
                    new RefreshTokenRequestDto { RefreshToken = originalRefreshToken }));
                await Assert.ThrowsAsync<UnauthorizedAccessException>(() => auth.LoginAsync(
                    new LoginDto { Identifier = email, Password = OldPassword }));

                var newLogin = await auth.LoginAsync(new LoginDto { Identifier = email, Password = NewPassword });
                Assert.NotEqual(originalAccessToken, newLogin.Token);
                var newSession = await authRepository.GetSessionInfoAsync(userId);
                Assert.Equal(newLogin.Token, newSession.Token);

                await Assert.ThrowsAsync<UnauthorizedAccessException>(() => reset.ResetPasswordAsync(
                    ResetRequest(firstResetToken, NewPassword)));

                var rollbackToken = Guid.NewGuid().ToString("N");
                var rollbackRefreshToken = Guid.NewGuid().ToString("N");
                context.PasswordResetCodes.Add(NewResetCode(userId, rollbackToken));
                context.RefreshTokens.Add(new RefreshToken
                {
                    UserId = userId,
                    Token = rollbackRefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddHours(1)
                });
                await context.SaveChangesAsync();

                var tokenBeforeRollback = (await authRepository.GetSessionInfoAsync(userId)).Token;
                await using (var failingContext = new AppDbContext(CreateOptions(
                    safeConnectionString, new FailPasswordResetUpdateInterceptor())))
                {
                    var failingReset = CreatePasswordResetService(failingContext);
                    await Assert.ThrowsAsync<InvalidOperationException>(() => failingReset.ResetPasswordAsync(
                        ResetRequest(rollbackToken, "RollbackSyntheticPass-0!")));
                }

                await using var afterRollback = new AppDbContext(options);
                var userAfterRollback = await afterRollback.Users.SingleAsync(user => user.Id == userId);
                var resetAfterRollback = await afterRollback.PasswordResetCodes
                    .SingleAsync(code => code.ResetToken == rollbackToken);
                var refreshAfterRollback = await afterRollback.RefreshTokens
                    .SingleAsync(token => token.Token == rollbackRefreshToken);
                Assert.True(BCrypt.Net.BCrypt.Verify(NewPassword, userAfterRollback.PasswordHash));
                Assert.Equal(tokenBeforeRollback, userAfterRollback.Token);
                Assert.False(resetAfterRollback.IsCompleted);
                Assert.False(refreshAfterRollback.IsRevoked);

                await VerifySerializableRetryAsync(options, userId);
            }

            // Separate DbContexts and separate process-local locks model two API instances.
            var concurrentResetToken = Guid.NewGuid().ToString("N");
            var concurrentRefreshToken = Guid.NewGuid().ToString("N");
            await using (var setupConcurrent = new AppDbContext(options))
            {
                setupConcurrent.PasswordResetCodes.Add(NewResetCode(userId, concurrentResetToken));
                setupConcurrent.RefreshTokens.Add(new RefreshToken
                {
                    UserId = userId,
                    Token = concurrentRefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddHours(1)
                });
                await setupConcurrent.SaveChangesAsync();
            }

            var startRace = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var resetTask = RunAfterSignalAsync(startRace.Task, async () =>
            {
                await using var context = new AppDbContext(options);
                await CreatePasswordResetService(context).ResetPasswordAsync(
                    ResetRequest(concurrentResetToken, ConcurrentPassword));
            });
            var refreshTask = RunAfterSignalAsync(startRace.Task, async () =>
            {
                await using var context = new AppDbContext(options);
                await CreateAuthService(context).RefreshTokenAsync(
                    new RefreshTokenRequestDto { RefreshToken = concurrentRefreshToken });
            }, allowRejectedRefresh: true);
            startRace.TrySetResult();
            await Task.WhenAll(resetTask, refreshTask);

            await using (var verifyRace = new AppDbContext(options))
            {
                var userAfterRace = await verifyRace.Users.SingleAsync(user => user.Id == userId);
                var codeAfterRace = await verifyRace.PasswordResetCodes
                    .SingleAsync(code => code.ResetToken == concurrentResetToken);
                var activeRefreshCount = await verifyRace.RefreshTokens
                    .CountAsync(token => token.UserId == userId && !token.IsRevoked && token.ExpiresAt > DateTime.UtcNow);
                Assert.True(BCrypt.Net.BCrypt.Verify(ConcurrentPassword, userAfterRace.PasswordHash));
                Assert.Null(userAfterRace.Token);
                Assert.True(codeAfterRace.IsCompleted);
                Assert.Equal(0, activeRefreshCount);

                var finalAuth = CreateAuthService(verifyRace);
                await Assert.ThrowsAsync<UnauthorizedAccessException>(() => finalAuth.LoginAsync(
                    new LoginDto { Identifier = email, Password = OldPassword }));
                var finalLogin = await finalAuth.LoginAsync(new LoginDto
                {
                    Identifier = email,
                    Password = ConcurrentPassword
                });
                Assert.NotEqual(originalAccessToken, finalLogin.Token);
                Assert.Equal(finalLogin.Token,
                    (await new AuthRepository(verifyRace).GetSessionInfoAsync(userId)).Token);
            }
        }
        finally
        {
            if (userId != 0)
            {
                await using var cleanup = new AppDbContext(options);
                await cleanup.PasswordResetCodes.Where(code => code.UserId == userId).ExecuteDeleteAsync();
                await cleanup.RefreshTokens.Where(token => token.UserId == userId).ExecuteDeleteAsync();
                await cleanup.UserRoles.Where(role => role.UserId == userId).ExecuteDeleteAsync();
                await cleanup.Users.Where(user => user.Id == userId).ExecuteDeleteAsync();
                if (managerRoleCreated)
                    await cleanup.Roles.Where(role => role.Name == RoleNames.Gerente).ExecuteDeleteAsync();
            }
        }
    }

    private static async Task VerifySerializableRetryAsync(DbContextOptions<AppDbContext> options, int userId)
    {
        var bothRead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstArrivals = 0;
        async Task<int> RunIncrementAsync(AppDbContext context)
        {
            var localRuns = 0;
            var runner = new EfAuthTransactionRunner(context);
            return await runner.ExecuteSerializableAsync(async () =>
            {
                var run = Interlocked.Increment(ref localRuns);
                var user = await context.Users.SingleAsync(candidate => candidate.Id == userId);
                if (run == 1)
                {
                    if (Interlocked.Increment(ref firstArrivals) == 2)
                        bothRead.TrySetResult();
                    await bothRead.Task.WaitAsync(TimeSpan.FromSeconds(20));
                }

                user.FailedLoginAttempts++;
                await context.SaveChangesAsync();
                return run;
            });
        }

        await using var contextA = new AppDbContext(options);
        await using var contextB = new AppDbContext(options);
        var attempts = await Task.WhenAll(
            RunIncrementAsync(contextA),
            RunIncrementAsync(contextB));
        // ExecuteSerializableAsync returns the last callback result; it must exceed one
        // for at least one transaction after PostgreSQL aborts its conflicting snapshot.
        Assert.Contains(attempts, attempt => attempt > 1);

        await using var verify = new AppDbContext(options);
        Assert.Equal(2, await verify.Users.Where(user => user.Id == userId)
            .Select(user => user.FailedLoginAttempts).SingleAsync());
    }

    private static async Task AssertAccessTokenRejectedAfterRecoveryAsync(
        AppDbContext db,
        int userId,
        string oldAccessToken)
    {
        var settings = new JwtSettings
        {
            SecretKey = "test-only-signing-key-with-more-than-32-bytes",
            Issuer = "SaborExpress.HU01.Postgres.Tests",
            Audience = "SaborExpress.HU01.Postgres.Tests",
            ExpirationInMinutes = 30,
            RefreshTokenExpirationHours = 12
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Auth:SingleSession"] = "true",
            ["JwtSettings:SecretKey"] = settings.SecretKey,
            ["JwtSettings:Issuer"] = settings.Issuer,
            ["JwtSettings:Audience"] = settings.Audience
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<IAuthRepository>(new AuthRepository(db));
        JwtConfiguration.AddJwtConfiguration(services, configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        var validationContext = new TokenValidatedContext(
            new DefaultHttpContext { RequestServices = provider },
            new AuthenticationScheme(
                JwtBearerDefaults.AuthenticationScheme,
                JwtBearerDefaults.AuthenticationScheme,
                typeof(JwtBearerHandler)),
            options)
        {
            Principal = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "Bearer")),
            SecurityToken = new JwtSecurityTokenHandler().ReadJwtToken(oldAccessToken)
        };

        await options.Events.OnTokenValidated(validationContext);

        Assert.NotNull(validationContext.Result?.Failure);
    }

    private static async Task RunAfterSignalAsync(Task signal, Func<Task> operation, bool allowRejectedRefresh = false)
    {
        await signal;
        try
        {
            await operation();
        }
        catch (UnauthorizedAccessException) when (allowRejectedRefresh)
        {
            // A refresh that loses to reset must be rejected; one that commits first is
            // subsequently revoked by the reset. Both orderings must leave no active token.
        }
    }

    private static DbContextOptions<AppDbContext> CreateOptions(
        string connectionString,
        params IInterceptor[] interceptors)
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connectionString);
        if (interceptors.Length > 0)
            builder.AddInterceptors(interceptors);
        return builder.Options;
    }

    private static PasswordResetCode NewResetCode(int userId, string resetToken) => new()
    {
        UserId = userId,
        Code = "731904",
        CodeExpiresAt = DateTime.UtcNow.AddMinutes(5),
        IsUsed = true,
        ResetToken = resetToken,
        ResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(15),
        IsCompleted = false,
        CreatedAt = DateTime.UtcNow
    };

    private static ResetPasswordDto ResetRequest(string resetToken, string newPassword) => new()
    {
        ResetToken = resetToken,
        NewPassword = newPassword,
        ConfirmPassword = newPassword
    };

    private static PasswordResetService CreatePasswordResetService(AppDbContext context) => new(
        new AuthRepository(context),
        new PasswordResetRepository(context),
        new RefreshTokenRepository(context),
        new UserSessionLock(),
        new EfAuthTransactionRunner(context),
        Mock.Of<INotificationService>(),
        Mock.Of<IBackgroundEmailQueue>(),
        new ForgotPasswordValidator(),
        new VerifyResetCodeValidator(),
        new ResetPasswordValidator());

    private static AuthService CreateAuthService(AppDbContext context)
    {
        var settings = new JwtSettings
        {
            SecretKey = "test-only-signing-key-with-more-than-32-bytes",
            Issuer = "SaborExpress.HU01.Postgres.Tests",
            Audience = "SaborExpress.HU01.Postgres.Tests",
            ExpirationInMinutes = 30,
            RefreshTokenExpirationHours = 12
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Auth:SingleSession"] = "true",
            ["JwtSettings:SecretKey"] = settings.SecretKey,
            ["JwtSettings:Issuer"] = settings.Issuer,
            ["JwtSettings:Audience"] = settings.Audience
        }).Build();
        var notifier = new Mock<IRealtimeNotifier>();
        notifier.Setup(service => service.SessionReplacedAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        var notificationRepository = new Mock<INotificationRepository>();
        notificationRepository.Setup(repository => repository.RemovePushDevicesByUserIdAsync(It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        return new AuthService(
            new AuthRepository(context),
            new LoginValidator(),
            new JwtTokenGenerator(Options.Create(settings)),
            Options.Create(settings),
            Mock.Of<INotificationService>(),
            notifier.Object,
            new RefreshTokenRepository(context),
            Mock.Of<IEmployeeScheduleRepository>(),
            Mock.Of<ICustomerRepository>(),
            notificationRepository.Object,
            config,
            Mock.Of<IBranchRepository>(),
            context,
            new UserSessionLock(),
            new EfAuthTransactionRunner(context));
    }

    private sealed class FailPasswordResetUpdateInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context?.ChangeTracker.Entries<PasswordResetCode>()
                .Any(entry => entry.State == EntityState.Modified) == true)
            {
                throw new InvalidOperationException("Controlled HU-01 rollback injection.");
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
#endif

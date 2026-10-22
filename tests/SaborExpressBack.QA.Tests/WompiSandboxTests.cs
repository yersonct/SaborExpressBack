using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SaborExpress.Modules.Auth.Interfaces;
using SaborExpress.Modules.Customers.Interfaces;
using SaborExpress.Modules.Deliveries.Interfaces;
using SaborExpress.Modules.Employees.Interfaces;
using SaborExpress.Modules.Notifications.DTOs;
using SaborExpress.Modules.Notifications.Interfaces;
using SaborExpress.Modules.Orders.Enum;
using SaborExpress.Modules.Orders.Interfaces;
using SaborExpress.Modules.Orders.Models;
using SaborExpress.Modules.Payments.DTOs;
using SaborExpress.Modules.Payments.Enum;
using SaborExpress.Modules.Payments.Interfaces;
using SaborExpress.Modules.Payments.Models;
using SaborExpress.Modules.Payments.Services;
using SaborExpress.Modules.Payments.Validators;
using SaborExpress.Modules.Tables.Interfaces;
using SaborExpress.Shared.Interfaces;
using Xunit;

namespace SaborExpressBack.QA.Tests;

public sealed class WompiSandboxTests
{
    private const string Reference = "SABOREXPRESS-71-synthetic";
    private const string TransactionId = "tx-synthetic-71";

    [Fact]
    public void WebhookSignature_UsesThePropertiesDeclaredByTheEvent()
    {
        var eventSecret = new string('s', 48);
        var rawBody = CreateSignedWebhook(eventSecret, Reference, TransactionId, "APPROVED", 125_000, "COP");
        var client = CreateWompiClient(eventSecret, new RecordingHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)));

        Assert.True(client.VerifyWebhookSignature(rawBody, string.Empty));
        Assert.False(client.VerifyWebhookSignature(rawBody.Replace("125000", "125001", StringComparison.Ordinal), string.Empty));
    }

    [Fact]
    public void WebhookSignature_RejectsDifferentBodyAndHeaderChecksums()
    {
        var eventSecret = new string('e', 48);
        var rawBody = CreateSignedWebhook(eventSecret, Reference, TransactionId, "APPROVED", 125_000, "COP");
        var client = CreateWompiClient(eventSecret, new RecordingHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)));

        Assert.False(client.VerifyWebhookSignature(rawBody, new string('0', 64)));
    }

    [Fact]
    public async Task WompiClient_QueriesTheConfiguredSandboxAndReadsCurrency()
    {
        HttpRequestMessage? observedRequest = null;
        var handler = new RecordingHttpMessageHandler(request =>
        {
            observedRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {"data":[
                      {"id":"tx-declined","reference":"SABOREXPRESS-71-synthetic","status":"DECLINED","amount_in_cents":125000,"currency":"COP"},
                      {"id":"tx-approved","reference":"SABOREXPRESS-71-synthetic","status":"APPROVED","amount_in_cents":125000,"currency":"COP"}
                    ]}
                    """, Encoding.UTF8, "application/json")
            };
        });
        var client = CreateWompiClient(new string('e', 48), handler);

        var result = await client.GetTransactionByReferenceAsync(Reference);

        Assert.NotNull(observedRequest);
        Assert.Equal("sandbox.wompi.co", observedRequest!.RequestUri!.Host);
        Assert.Equal("v1/transactions?reference=SABOREXPRESS-71-synthetic", observedRequest.RequestUri.PathAndQuery.TrimStart('/'));
        Assert.Equal("tx-approved", result!.Id);
        Assert.Equal(Reference, result.Reference);
        Assert.Equal(125_000, result.AmountInCents);
        Assert.Equal("COP", result.Currency);
    }

    [Theory]
    [InlineData(null, "COP", Reference, 125_000, true)]
    [InlineData("USD", "USD", Reference, 125_000, false)]
    [InlineData("COP", "COP", "OTHER-REFERENCE", 125_000, false)]
    [InlineData("COP", "COP", Reference, 125_001, false)]
    public void AuthoritativeTransactionMustMatchCurrencyReferenceAndAmount(
        string? webhookCurrency, string authoritativeCurrency, string authoritativeReference,
        int authoritativeAmount, bool expected)
    {
        var webhook = new WompiTransactionDto
        {
            Id = TransactionId,
            Reference = Reference,
            Status = "APPROVED",
            Currency = webhookCurrency ?? string.Empty,
            AmountInCents = 125_000
        };
        var authoritative = new WompiTransactionDto
        {
            Id = TransactionId,
            Reference = authoritativeReference,
            Status = "APPROVED",
            Currency = authoritativeCurrency,
            AmountInCents = authoritativeAmount
        };

        Assert.Equal(expected, WompiTransactionConfirmation.MatchesAuthoritativeTransaction(webhook, authoritative));
    }

    [Theory]
    [InlineData("APPROVED", PaymentStatus.Completed)]
    [InlineData("DECLINED", PaymentStatus.Failed)]
    [InlineData("ERROR", PaymentStatus.Failed)]
    [InlineData("VOIDED", PaymentStatus.Failed)]
    [InlineData("PENDING", PaymentStatus.Pending)]
    // Wompi's documented transaction states do not include EXPIRED. Treat an
    // unknown/expired response as non-final so it cannot confirm or release an order.
    [InlineData("EXPIRED", PaymentStatus.Pending)]
    public async Task Webhook_MapsSandboxStatusesWithoutConfirmingNonFinalResults(
        string upstreamStatus, PaymentStatus expectedPaymentStatus)
    {
        var harness = new PaymentHarness(upstreamStatus);

        await harness.Service.ProcessWompiWebhookAsync(
            harness.CreateWebhook(upstreamStatus), "synthetic-signed-body", "synthetic-checksum");

        Assert.Equal(expectedPaymentStatus, harness.Payment.Status);
        if (expectedPaymentStatus == PaymentStatus.Completed)
        {
            Assert.Equal(OrderStatus.Confirmed, harness.Payment.Order.Status);
            harness.PaymentRepository.Verify(repository => repository.TryTransitionWompiStatusAsync(
                harness.Payment.Id, PaymentStatus.Completed, TransactionId), Times.Once);
        }
        else if (expectedPaymentStatus == PaymentStatus.Failed)
        {
            harness.PaymentRepository.Verify(repository => repository.TryTransitionWompiStatusAsync(
                harness.Payment.Id, PaymentStatus.Failed, TransactionId), Times.Once);
            harness.OrderRepository.Verify(repository => repository.UpdateAsync(It.IsAny<Order>()), Times.Never);
        }
        else
        {
            harness.PaymentRepository.Verify(repository => repository.TryTransitionWompiStatusAsync(
                It.IsAny<int>(), It.IsAny<PaymentStatus>(), It.IsAny<string>()), Times.Never);
            harness.OrderRepository.Verify(repository => repository.UpdateAsync(It.IsAny<Order>()), Times.Never);
        }
    }

    [Fact]
    public async Task Webhook_RejectsAmountMismatchBeforeChangingPayment()
    {
        var harness = new PaymentHarness("APPROVED", amountInCents: 125_001);

        await harness.Service.ProcessWompiWebhookAsync(
            harness.CreateWebhook("APPROVED", amountInCents: 125_001), "synthetic-signed-body", "synthetic-checksum");

        Assert.Equal(PaymentStatus.Pending, harness.Payment.Status);
        harness.PaymentRepository.Verify(repository => repository.TryTransitionWompiStatusAsync(
            It.IsAny<int>(), It.IsAny<PaymentStatus>(), It.IsAny<string>()), Times.Never);
        harness.OrderRepository.Verify(repository => repository.UpdateAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task Webhook_RejectsAuthoritativeCurrencyMismatchBeforeChangingPayment()
    {
        var harness = new PaymentHarness("APPROVED", currency: "USD");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => harness.Service.ProcessWompiWebhookAsync(
            harness.CreateWebhook("APPROVED"), "synthetic-signed-body", "synthetic-checksum"));

        Assert.Equal(PaymentStatus.Pending, harness.Payment.Status);
        harness.PaymentRepository.Verify(repository => repository.TryTransitionWompiStatusAsync(
            It.IsAny<int>(), It.IsAny<PaymentStatus>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task WebhookWithAnotherOrderReference_DoesNotFindOrConfirmPayment()
    {
        var harness = new PaymentHarness("APPROVED");
        var webhook = harness.CreateWebhook("APPROVED");
        webhook.Data.Transaction.Reference = "SABOREXPRESS-OTHER-SYNTHETIC";

        await harness.Service.ProcessWompiWebhookAsync(webhook, "synthetic-signed-body", "synthetic-checksum");

        Assert.Equal(PaymentStatus.Pending, harness.Payment.Status);
        harness.PaymentRepository.Verify(repository => repository.GetByWompiReferenceAsync(
            "SABOREXPRESS-OTHER-SYNTHETIC"), Times.Once);
        harness.WompiClient.Verify(client => client.GetTransactionByReferenceAsync(It.IsAny<string>()), Times.Never);
        harness.PaymentRepository.Verify(repository => repository.TryTransitionWompiStatusAsync(
            It.IsAny<int>(), It.IsAny<PaymentStatus>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task BackgroundReconciliation_DoesNotConfirmNonCopTransaction()
    {
        var harness = new PaymentHarness("APPROVED", currency: "USD");
        harness.PaymentRepository.Setup(repository => repository.GetPendingWompiSinceAsync(It.IsAny<DateTime>()))
            .ReturnsAsync([harness.Payment]);
        harness.PaymentRepository.Setup(repository => repository.GetWompiPostProcessingPendingAsync(It.IsAny<int>()))
            .ReturnsAsync([]);

        await harness.Service.SyncPendingWompiPaymentsAsync();

        Assert.Equal(PaymentStatus.Pending, harness.Payment.Status);
        harness.PaymentRepository.Verify(repository => repository.TryTransitionWompiStatusAsync(
            It.IsAny<int>(), It.IsAny<PaymentStatus>(), It.IsAny<string>()), Times.Never);
        harness.OrderRepository.Verify(repository => repository.UpdateAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task RepeatedApprovedWebhook_TransitionsAndRunsEffectsOnlyOnce()
    {
        var harness = new PaymentHarness("APPROVED");
        var webhook = harness.CreateWebhook("APPROVED");

        await harness.Service.ProcessWompiWebhookAsync(webhook, "synthetic-signed-body", "synthetic-checksum");
        await harness.Service.ProcessWompiWebhookAsync(webhook, "synthetic-signed-body", "synthetic-checksum");

        Assert.Equal(PaymentStatus.Completed, harness.Payment.Status);
        harness.PaymentRepository.Verify(repository => repository.TryTransitionWompiStatusAsync(
            harness.Payment.Id, PaymentStatus.Completed, TransactionId), Times.Once);
        harness.PaymentRepository.Verify(repository => repository.MarkWompiPostProcessingCompletedAsync(
            harness.Payment.Id, It.IsAny<DateTime>()), Times.Once);
        harness.OrderRepository.Verify(repository => repository.UpdateAsync(It.IsAny<Order>()), Times.Once);
        harness.TableAvailability.Verify(service => service.TryReleaseAfterConfirmationAsync(
            harness.Payment.OrderId, true), Times.Once);
    }

    [Fact]
    public async Task ApprovedPayment_RetriesWhenCustomerNotificationFails()
    {
        var harness = new PaymentHarness("APPROVED", customerId: 93);
        var notificationAttempts = 0;
        harness.CustomerRepository.Setup(repository => repository.GetUserIdByCustomerIdAsync(93))
            .ReturnsAsync(903);
        harness.Notifications.Setup(service => service.CreateAsync(
                903, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SaborExpress.Modules.Notifications.Enum.NotificationType>(),
                It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string?>()))
            .Returns(() => ++notificationAttempts == 1
                ? Task.FromException<NotificationDto>(new InvalidOperationException("synthetic notification failure"))
                : Task.FromResult(new NotificationDto()));

        await Assert.ThrowsAsync<InvalidOperationException>(() => harness.Service.ProcessWompiWebhookAsync(
            harness.CreateWebhook("APPROVED"), "synthetic-signed-body", "synthetic-checksum"));
        Assert.Equal(PaymentStatus.Completed, harness.Payment.Status);
        harness.PaymentRepository.Verify(repository => repository.ReleaseWompiPostProcessingClaimAsync(
            harness.Payment.Id), Times.Once);
        harness.PaymentRepository.Verify(repository => repository.MarkWompiPostProcessingCompletedAsync(
            harness.Payment.Id, It.IsAny<DateTime>()), Times.Never);

        await harness.RetryPostProcessingAsync();

        Assert.Equal(2, notificationAttempts);
        harness.PaymentRepository.Verify(repository => repository.MarkWompiPostProcessingCompletedAsync(
            harness.Payment.Id, It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task ApprovedPayment_RetriesWhenTableReleaseFails()
    {
        var harness = new PaymentHarness("APPROVED");
        var releaseAttempts = 0;
        harness.TableAvailability.Setup(service => service.TryReleaseAfterConfirmationAsync(
                harness.Payment.OrderId, true))
            .Returns(() => ++releaseAttempts == 1
                ? Task.FromException<bool>(new InvalidOperationException("synthetic table release failure"))
                : Task.FromResult(true));

        await Assert.ThrowsAsync<InvalidOperationException>(() => harness.Service.ProcessWompiWebhookAsync(
            harness.CreateWebhook("APPROVED"), "synthetic-signed-body", "synthetic-checksum"));
        Assert.Equal(PaymentStatus.Completed, harness.Payment.Status);
        harness.PaymentRepository.Verify(repository => repository.MarkWompiPostProcessingCompletedAsync(
            harness.Payment.Id, It.IsAny<DateTime>()), Times.Never);

        await harness.RetryPostProcessingAsync();

        Assert.Equal(2, releaseAttempts);
        harness.PaymentRepository.Verify(repository => repository.MarkWompiPostProcessingCompletedAsync(
            harness.Payment.Id, It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task ApprovedPayment_RetriesOrderConfirmationWhenPersistenceFails()
    {
        var harness = new PaymentHarness("APPROVED");
        var updateAttempts = 0;
        harness.OrderRepository.Setup(repository => repository.UpdateAsync(harness.Payment.Order))
            .Returns(() => ++updateAttempts == 1
                ? Task.FromException(new InvalidOperationException("synthetic order update failure"))
                : Task.CompletedTask);

        await Assert.ThrowsAsync<InvalidOperationException>(() => harness.Service.ProcessWompiWebhookAsync(
            harness.CreateWebhook("APPROVED"), "synthetic-signed-body", "synthetic-checksum"));

        Assert.Equal(PaymentStatus.Completed, harness.Payment.Status);
        Assert.Equal(OrderStatus.Pending, harness.Payment.Order.Status);
        harness.PaymentRepository.Verify(repository => repository.MarkWompiPostProcessingCompletedAsync(
            harness.Payment.Id, It.IsAny<DateTime>()), Times.Never);

        await harness.RetryPostProcessingAsync();

        Assert.Equal(2, updateAttempts);
        Assert.Equal(OrderStatus.Confirmed, harness.Payment.Order.Status);
        harness.PaymentRepository.Verify(repository => repository.MarkWompiPostProcessingCompletedAsync(
            harness.Payment.Id, It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task PaymentInitialization_UsesOrderTotalAndBuildsCopReferenceAndSignature()
    {
        var harness = new PaymentHarness("PENDING");
        var currentUser = new SaborExpress.Modules.Auth.Models.User
        {
            Id = 12,
            Customer = new SaborExpress.Modules.Customers.Models.Customer { Id = 93 }
        };
        harness.OrderRepository.Setup(repository => repository.GetByIdAsync(71))
            .ReturnsAsync(new Order
            {
                Id = 71,
                CustomerId = 93,
                Total = 1_250.75m,
                Status = OrderStatus.Pending
            });
        harness.AuthRepository.Setup(repository => repository.GetByIdWithRelationsAsync(12))
            .ReturnsAsync(currentUser);
        harness.PaymentRepository.Setup(repository => repository.GetByOrderIdAsync(71))
            .ReturnsAsync([]);
        harness.PaymentRepository.Setup(repository => repository.GetOrCreatePendingWompiPaymentAsync(
                It.IsAny<Payment>(), OrderStatus.Pending))
            .ReturnsAsync((Payment requested, OrderStatus? _) =>
            {
                requested.Id = 91;
                harness.Payment = requested;
                return (requested, true);
            });

        var widget = await harness.Service.InitWompiPaymentAsync(
            new InitWompiPaymentDto { OrderId = 71, Amount = 1m }, currentUser.Id);

        Assert.Equal(125_075, widget.AmountInCents);
        Assert.Equal("COP", widget.Currency);
        Assert.StartsWith("SABOREXPRESS-71-", widget.Reference, StringComparison.Ordinal);
        Assert.Equal(widget.Reference, harness.Payment.WompiReference);
        harness.WompiClient.Verify(client => client.BuildIntegritySignature(
            widget.Reference, 125_075, "COP"), Times.Once);
    }

    private static string CreateSignedWebhook(
        string eventSecret, string reference, string transactionId, string status, int amountInCents, string currency)
    {
        const long timestamp = 1_798_000_000;
        var values = new[] { transactionId, status, amountInCents.ToString(), currency, reference };
        var signatureInput = string.Concat(values) + timestamp + eventSecret;
        var checksum = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(signatureInput))).ToLowerInvariant();
        return $$$"""
            {
              "event":"transaction.updated",
              "data":{"transaction":{"id":"{{{transactionId}}}","status":"{{{status}}}","amount_in_cents":{{{amountInCents}}},"currency":"{{{currency}}}","reference":"{{{reference}}}"}},
              "signature":{"properties":["transaction.id","transaction.status","transaction.amount_in_cents","transaction.currency","transaction.reference"],"checksum":"{{{checksum}}}"},
              "timestamp":{{{timestamp}}}
            }
            """;
    }

    private static WompiClient CreateWompiClient(string eventSecret, HttpMessageHandler handler)
    {
        var randomLikeValue = new string('x', 48);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Wompi:BaseUrl"] = "https://sandbox.wompi.co/v1/",
                ["Wompi:PublicKey"] = "pub_test_" + randomLikeValue,
                ["Wompi:PrivateKey"] = "prv_test_" + new string('y', 48),
                ["Wompi:IntegritySecret"] = "test_integrity_" + new string('z', 48),
                ["Wompi:EventsSecret"] = eventSecret
            })
            .Build();

        return new WompiClient(new HttpClient(handler), config);
    }

    private sealed class RecordingHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }

    private sealed class PaymentHarness
    {
        private bool _postProcessingClaimed;
        private bool _postProcessingCompleted;

        public Payment Payment { get; set; }
        public Mock<IPaymentRepository> PaymentRepository { get; } = new();
        public Mock<IOrderRepository> OrderRepository { get; } = new();
        public Mock<IWompiClient> WompiClient { get; } = new();
        public Mock<IAuthRepository> AuthRepository { get; } = new();
        public Mock<ICustomerRepository> CustomerRepository { get; } = new();
        public Mock<IUserNotificationService> Notifications { get; } = new();
        public Mock<ITableAvailabilityService> TableAvailability { get; } = new();
        public PaymentService Service { get; }

        public PaymentHarness(string status, int amountInCents = 125_000, string currency = "COP", int? customerId = null)
        {
            var order = new Order
            {
                Id = 71,
                CustomerId = customerId,
                BranchId = 4,
                Total = 1_250m,
                Status = OrderStatus.Pending,
                OrderType = SaborExpress.Modules.Orders.Enum.OrderType.ToGo
            };
            Payment = new Payment
            {
                Id = 91,
                OrderId = order.Id,
                Order = order,
                Amount = order.Total,
                Method = PaymentMethod.Wompi,
                Status = PaymentStatus.Pending,
                WompiReference = Reference
            };
            order.Payments.Add(Payment);

            var transaction = new WompiTransactionDto
            {
                Id = TransactionId,
                Reference = Reference,
                Status = status,
                AmountInCents = amountInCents,
                Currency = currency
            };
            WompiClient.Setup(client => client.VerifyWebhookSignature(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);
            WompiClient.Setup(client => client.GetTransactionByReferenceAsync(Reference))
                .ReturnsAsync(transaction);

            PaymentRepository.Setup(repository => repository.GetByWompiReferenceAsync(Reference))
                .ReturnsAsync(() => Payment);
            PaymentRepository.Setup(repository => repository.GetByIdAsync(Payment.Id))
                .ReturnsAsync(() => Payment);
            PaymentRepository.Setup(repository => repository.TryTransitionWompiStatusAsync(
                    Payment.Id, It.IsAny<PaymentStatus>(), TransactionId))
                .ReturnsAsync((int _, PaymentStatus newStatus, string _) =>
                {
                    if (Payment.Status != PaymentStatus.Pending)
                        return false;
                    Payment.Status = newStatus;
                    Payment.WompiTransactionId = TransactionId;
                    return true;
                });
            PaymentRepository.Setup(repository => repository.TryClaimWompiPostProcessingAsync(
                    Payment.Id, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(() =>
                {
                    if (_postProcessingClaimed || _postProcessingCompleted)
                        return false;
                    _postProcessingClaimed = true;
                    return true;
                });
            PaymentRepository.Setup(repository => repository.MarkWompiPostProcessingCompletedAsync(
                    Payment.Id, It.IsAny<DateTime>()))
                .Callback(() =>
                {
                    _postProcessingCompleted = true;
                    _postProcessingClaimed = false;
                })
                .Returns(Task.CompletedTask);
            PaymentRepository.Setup(repository => repository.ReleaseWompiPostProcessingClaimAsync(Payment.Id))
                .Callback(() => _postProcessingClaimed = false)
                .Returns(Task.CompletedTask);
            PaymentRepository.Setup(repository => repository.GetPendingWompiSinceAsync(It.IsAny<DateTime>()))
                .ReturnsAsync([]);
            PaymentRepository.Setup(repository => repository.GetWompiPostProcessingPendingAsync(It.IsAny<int>()))
                .ReturnsAsync(() => _postProcessingCompleted ? [] : [Payment]);
            OrderRepository.Setup(repository => repository.UpdateAsync(It.IsAny<Order>()))
                .Returns(Task.CompletedTask);
            OrderRepository.Setup(repository => repository.SaveChangesAsync())
                .Returns(Task.CompletedTask);
            CustomerRepository.Setup(repository => repository.GetUserIdByCustomerIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int?)null);
            Notifications.Setup(service => service.CreateAsync(
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<SaborExpress.Modules.Notifications.Enum.NotificationType>(),
                    It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<string?>()))
                .ReturnsAsync(new NotificationDto());
            TableAvailability.Setup(service => service.TryReleaseAfterConfirmationAsync(
                    It.IsAny<int>(), It.IsAny<bool>()))
                .ReturnsAsync(true);
            var realtime = new Mock<IRealtimeNotifier>();
            realtime.Setup(service => service.PaymentsChangedAsync()).Returns(Task.CompletedTask);
            realtime.Setup(service => service.OrdersChangedAsync()).Returns(Task.CompletedTask);
            realtime.Setup(service => service.TablesChangedAsync()).Returns(Task.CompletedTask);

            var validator = new PaymentValidator(PaymentRepository.Object, OrderRepository.Object);
            Service = new PaymentService(
                PaymentRepository.Object,
                validator,
                AuthRepository.Object,
                Mock.Of<SaborExpress.Shared.Interfaces.IAuthorizationService>(),
                WompiClient.Object,
                OrderRepository.Object,
                TableAvailability.Object,
                Mock.Of<IDeliveryAssignmentService>(),
                Notifications.Object,
                CustomerRepository.Object,
                Mock.Of<IEmployeeRepository>(),
                NullLogger<PaymentService>.Instance,
                realtime.Object);
        }

        public WompiWebhookDto CreateWebhook(string status, int? amountInCents = null) => new()
        {
            Event = "transaction.updated",
            Data = new WompiWebhookDataDto
            {
                Transaction = new WompiTransactionDto
                {
                    Id = TransactionId,
                    Reference = Reference,
                    Status = status,
                    AmountInCents = amountInCents ?? 125_000,
                    Currency = "COP"
                }
            }
        };

        public Task RetryPostProcessingAsync() => Service.SyncPendingWompiPaymentsAsync();
    }
}

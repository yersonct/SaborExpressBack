using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SaborExpress.Modules.Invoices.DTOs;
using SaborExpress.Modules.Invoices.Interfaces;
using SaborExpress.Modules.Invoices.Models;
using SaborExpress.Modules.Invoices.Services;
using SaborExpress.Modules.Invoices.Validators;
using SaborExpress.Modules.Notifications.Hubs;
using SaborExpress.Modules.Payments.Enum;
using SaborExpress.Modules.Payments.Interfaces;
using SaborExpress.Modules.Payments.Models;
using SaborExpress.Shared.Services;
using Xunit;

namespace SaborExpressBack.QA.Tests;

public sealed class InvoiceAndRealtimeTests
{
    [Fact]
    public async Task RepeatedInvoiceRequest_ReturnsExistingReceiptWithoutInsertingAnother()
    {
        const int paymentId = 22;
        var paymentRepository = new Mock<IPaymentRepository>();
        paymentRepository.Setup(repository => repository.GetByIdAsync(paymentId))
            .ReturnsAsync(new Payment { Id = paymentId, Status = PaymentStatus.Completed });

        var existing = new Invoice
        {
            Id = 34,
            PaymentId = paymentId,
            OrderId = 18,
            BranchId = 3,
            InvoiceNumber = "QA-SYNTHETIC-34"
        };
        var invoiceRepository = new Mock<IInvoiceRepository>();
        invoiceRepository.Setup(repository => repository.GetByPaymentIdAsync(paymentId))
            .ReturnsAsync(existing);

        var service = new InvoiceService(
            invoiceRepository.Object,
            paymentRepository.Object,
            new InvoiceValidator(paymentRepository.Object));

        var result = await service.CreateAsync(new CreateInvoiceDto { PaymentId = paymentId });

        Assert.Equal(existing.Id, result.Id);
        Assert.Equal(existing.InvoiceNumber, result.InvoiceNumber);
        invoiceRepository.Verify(repository => repository.AddWithAssignedNumberAsync(It.IsAny<Invoice>()), Times.Never);
    }

    [Fact]
    public async Task OrderChange_PublishesRealtimeEventUsedByConnectedClients()
    {
        var notificationProxy = new Mock<IClientProxy>();
        notificationProxy
            .Setup(proxy => proxy.SendCoreAsync("OrdersChanged", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var notificationClients = new Mock<IHubClients>();
        notificationClients.SetupGet(clients => clients.All).Returns(notificationProxy.Object);
        var notificationHub = new Mock<IHubContext<NotificationHub>>();
        notificationHub.SetupGet(context => context.Clients).Returns(notificationClients.Object);

        var publicMenuProxy = new Mock<IClientProxy>();
        publicMenuProxy
            .Setup(proxy => proxy.SendCoreAsync("KitchenBoardChanged", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var publicMenuClients = new Mock<IHubClients>();
        publicMenuClients.SetupGet(clients => clients.All).Returns(publicMenuProxy.Object);
        var publicMenuHub = new Mock<IHubContext<PublicMenuHub>>();
        publicMenuHub.SetupGet(context => context.Clients).Returns(publicMenuClients.Object);

        var notifier = new RealtimeNotifier(
            notificationHub.Object,
            publicMenuHub.Object,
            NullLogger<RealtimeNotifier>.Instance);

        await notifier.OrdersChangedAsync();

        notificationProxy.Verify(proxy => proxy.SendCoreAsync(
            "OrdersChanged", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Once);
        publicMenuProxy.Verify(proxy => proxy.SendCoreAsync(
            "KitchenBoardChanged", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

using Moq;
using SaborExpress.Modules.Deliveries.DTOs;
using SaborExpress.Modules.Deliveries.Enum;
using SaborExpress.Modules.Deliveries.Interfaces;
using SaborExpress.Modules.Deliveries.Models;
using SaborExpress.Modules.Deliveries.Validators;
using SaborExpress.Modules.Orders.DTOs;
using SaborExpress.Modules.Orders.Enum;
using SaborExpress.Modules.Orders.Interfaces;
using SaborExpress.Modules.Orders.Models;
using SaborExpress.Modules.Orders.Validators;
using SaborExpress.Modules.Payments.DTOs;
using SaborExpress.Modules.Payments.Enum;
using SaborExpress.Modules.Payments.Interfaces;
using SaborExpress.Modules.Payments.Models;
using SaborExpress.Modules.Payments.Validators;
using Xunit;

namespace SaborExpressBack.QA.Tests;

public sealed class OperationalRulesTests
{
    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.InPreparation)]
    [InlineData(OrderStatus.InPreparation, OrderStatus.Ready)]
    [InlineData(OrderStatus.Ready, OrderStatus.Delivered)]
    [InlineData(OrderStatus.Pending, OrderStatus.Cancelled)]
    public void OrderStatus_ValidTransitionsAreAccepted(OrderStatus current, OrderStatus next)
    {
        var validator = new OrderValidator(Mock.Of<IOrderRepository>());
        validator.ValidateStatusChange(new Order { Status = current }, new UpdateOrderStatusDto { Status = next });
    }

    [Theory]
    [InlineData(OrderStatus.Delivered, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Pending, OrderStatus.Ready)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Delivered)]
    public void OrderStatus_InvalidTransitionsAreRejected(OrderStatus current, OrderStatus next)
    {
        var validator = new OrderValidator(Mock.Of<IOrderRepository>());
        Assert.Throws<ArgumentException>(() => validator.ValidateStatusChange(
            new Order { Status = current }, new UpdateOrderStatusDto { Status = next }));
    }

    [Fact]
    public void OrderCancellation_RequiresAReason()
    {
        var validator = new OrderValidator(Mock.Of<IOrderRepository>());
        Assert.Throws<ArgumentException>(() => validator.ValidateCancel(
            new Order { Status = OrderStatus.Pending }, new CancelOrderDto { Reason = "  " }));
    }

    [Fact]
    public async Task Payment_RejectsAmountAboveRemainingBalance()
    {
        var orderRepository = new Mock<IOrderRepository>();
        var paymentRepository = new Mock<IPaymentRepository>();
        orderRepository.Setup(repository => repository.GetByIdAsync(7))
            .ReturnsAsync(new Order { Id = 7, Status = OrderStatus.Confirmed, Total = 100m });
        paymentRepository.Setup(repository => repository.GetByOrderIdAsync(7))
            .ReturnsAsync([new Payment { Amount = 60m, Status = PaymentStatus.Completed, Method = PaymentMethod.Cash }]);
        var validator = new PaymentValidator(paymentRepository.Object, orderRepository.Object);

        var error = await Assert.ThrowsAsync<ArgumentException>(() => validator.ValidateCreateAsync(
            new CreatePaymentDto { OrderId = 7, Amount = 41m, Method = PaymentMethod.Cash }));
        Assert.Contains("excede", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Cancelled)]
    public async Task Payment_RejectsUnconfirmedOrCancelledOrder(OrderStatus status)
    {
        var orderRepository = new Mock<IOrderRepository>();
        orderRepository.Setup(repository => repository.GetByIdAsync(7))
            .ReturnsAsync(new Order { Id = 7, Status = status, Total = 100m });
        var paymentRepository = new Mock<IPaymentRepository>();
        var validator = new PaymentValidator(paymentRepository.Object, orderRepository.Object);

        await Assert.ThrowsAsync<ArgumentException>(() => validator.ValidateCreateAsync(
            new CreatePaymentDto { OrderId = 7, Amount = 100m, Method = PaymentMethod.Cash }));
    }

    [Fact]
    public void DeliveryStatus_OnlyMovesAssignedThenInTransitThenDelivered()
    {
        var validator = new DeliveryValidator(Mock.Of<IDeliveryRepository>(), Mock.Of<IOrderRepository>());
        var delivery = new Delivery { DeliveryPersonId = 88, Status = DeliveryStatus.Assigned };

        validator.ValidateStatusChange(delivery,
            new UpdateDeliveryStatusDto { Status = DeliveryStatus.InTransit }, 88, isAdmin: false);
        Assert.Throws<ArgumentException>(() => validator.ValidateStatusChange(delivery,
            new UpdateDeliveryStatusDto { Status = DeliveryStatus.Delivered }, 88, isAdmin: false));
        Assert.Throws<UnauthorizedAccessException>(() => validator.ValidateStatusChange(delivery,
            new UpdateDeliveryStatusDto { Status = DeliveryStatus.InTransit }, 99, isAdmin: false));
    }
}

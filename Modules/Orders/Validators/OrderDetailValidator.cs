// Modules/Orders/Validators/OrderDetailValidator.cs
using SaborExpress.Modules.Orders.DTOs;
using SaborExpress.Modules.Orders.Enum;
using SaborExpress.Modules.Orders.Interfaces;
using SaborExpress.Modules.Orders.Models;
using SaborExpress.Modules.Products.Interfaces; // ajusta el namespace si es distinto

namespace SaborExpress.Modules.Orders.Validators
{
    public class OrderDetailValidator
    {
        private readonly IOrderDetailRepository _orderDetailRepository;
        private readonly IProductRepository _productRepository;
        private readonly IOrderRepository _orderRepository; // NUEVO

        public OrderDetailValidator(
            IOrderDetailRepository orderDetailRepository,
            IProductRepository productRepository,
            IOrderRepository orderRepository) // NUEVO
        {
            _orderDetailRepository = orderDetailRepository;
            _productRepository = productRepository;
            _orderRepository = orderRepository; // NUEVO
        }

        public async Task ValidateCreateAsync(int orderId, CreateOrderDetailDto dto)
        {
            if (dto.ProductId <= 0)
                throw new ArgumentException("Debe indicar un producto válido");

            if (dto.Quantity <= 0)
                throw new ArgumentException("La cantidad debe ser mayor a cero");

            var order = await _orderRepository.GetByIdAsync(orderId); // NUEVO
            if (order == null)
                throw new ArgumentException("El pedido no existe");

            ValidateOrderIsEditable(order); // NUEVO

            var product = await _productRepository.GetByIdAsync(dto.ProductId);
            if (product == null)
                throw new ArgumentException("El producto no existe");

            if (!product.Status)
                throw new ArgumentException("El producto está agotado y no se puede agregar al pedido");
        }

        public async Task ValidateUpdate(OrderDetail orderDetail, UpdateOrderDetailDto dto) // ahora async
        {
            if (dto.Quantity <= 0)
                throw new ArgumentException("La cantidad debe ser mayor a cero");

            if (orderDetail.Status == OrderDetailStatus.Voided)
                throw new ArgumentException("No se puede modificar una línea anulada");

            if (orderDetail.Status == OrderDetailStatus.Delivered)
                throw new ArgumentException("No se puede modificar una línea ya entregada");

            var order = await _orderRepository.GetByIdAsync(orderDetail.OrderId); // NUEVO
            if (order == null)
                throw new ArgumentException("El pedido no existe");

            ValidateOrderIsEditable(order); // NUEVO
        }

        public async Task ValidateVoid(OrderDetail orderDetail, VoidOrderDetailDto dto) // ahora async
        {
            if (string.IsNullOrWhiteSpace(dto.Reason))
                throw new ArgumentException("Debe indicar el motivo de la anulación");

            if (orderDetail.Status == OrderDetailStatus.Voided)
                throw new ArgumentException("Esta línea ya fue anulada");

            if (orderDetail.Status == OrderDetailStatus.Delivered)
                throw new ArgumentException("No se puede anular una línea ya entregada");

            var order = await _orderRepository.GetByIdAsync(orderDetail.OrderId); // NUEVO
            if (order == null)
                throw new ArgumentException("El pedido no existe");

            ValidateOrderIsEditable(order); // NUEVO
        }

        // NUEVO: regla compartida por los 3 métodos
        private static void ValidateOrderIsEditable(Order order)
        {
            if (order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Cancelled)
                throw new ArgumentException(
                    $"No se pueden modificar las líneas de un pedido en estado {order.Status}");
        }
    }
}
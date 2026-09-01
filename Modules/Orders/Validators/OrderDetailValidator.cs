// Modules/Orders/Validators/OrderDetailValidator.cs
using SaborExpress.Modules.Orders.DTOs;
using SaborExpress.Modules.Orders.Enum;
using SaborExpress.Modules.Orders.Interfaces;
using SaborExpress.Modules.Orders.Models;
using SaborExpress.Modules.Products.Interfaces;
using SaborExpress.Modules.Products.Models;

namespace SaborExpress.Modules.Orders.Validators
{
    public class OrderDetailValidator
    {
        private readonly IOrderDetailRepository _orderDetailRepository;
        private readonly IProductRepository _productRepository;
        private readonly IOrderRepository _orderRepository;

        public OrderDetailValidator(
            IOrderDetailRepository orderDetailRepository,
            IProductRepository productRepository,
            IOrderRepository orderRepository)
        {
            _orderDetailRepository = orderDetailRepository;
            _productRepository = productRepository;
            _orderRepository = orderRepository;
        }

        // Devuelve el Product ya validado, para que el service no tenga que
        // volver a consultarlo (evita la consulta duplicada).
        public async Task<Product> ValidateCreateAsync(int orderId, CreateOrderDetailDto dto)
        {
            if (dto.ProductId <= 0)
                throw new ArgumentException("Debe indicar un producto valido");

            if (dto.Quantity <= 0)
                throw new ArgumentException("La cantidad debe ser mayor a cero");

            var orderStatus = await _orderRepository.GetOrderStatusAsync(orderId)
                ?? throw new ArgumentException("El pedido no existe");

            ValidateOrderIsEditable(orderStatus);

            var product = await _productRepository.GetByIdAsync(dto.ProductId)
                ?? throw new ArgumentException("El producto no existe");

            if (!product.Status)
                throw new ArgumentException("El producto esta agotado y no se puede agregar al pedido");

            return product;
        }

        public async Task ValidateUpdate(OrderDetail orderDetail, UpdateOrderDetailDto dto)
        {
            if (dto.Quantity <= 0)
                throw new ArgumentException("La cantidad debe ser mayor a cero");

            if (orderDetail.Status == OrderDetailStatus.Voided)
                throw new ArgumentException("No se puede modificar una linea anulada");

            if (orderDetail.Status == OrderDetailStatus.Delivered)
                throw new ArgumentException("No se puede modificar una linea ya entregada");

            var orderStatus = await _orderRepository.GetOrderStatusAsync(orderDetail.OrderId)
                ?? throw new ArgumentException("El pedido no existe");

            ValidateOrderIsEditable(orderStatus);
        }

        public async Task ValidateVoid(OrderDetail orderDetail, VoidOrderDetailDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Reason))
                throw new ArgumentException("Debe indicar el motivo de la anulacion");

            if (orderDetail.Status == OrderDetailStatus.Voided)
                throw new ArgumentException("Esta linea ya fue anulada");

            if (orderDetail.Status == OrderDetailStatus.Delivered)
                throw new ArgumentException("No se puede anular una linea ya entregada");

            var orderStatus = await _orderRepository.GetOrderStatusAsync(orderDetail.OrderId)
                ?? throw new ArgumentException("El pedido no existe");

            ValidateOrderIsEditable(orderStatus);
        }

        private static void ValidateOrderIsEditable(OrderStatus status)
        {
            if (status == OrderStatus.Delivered || status == OrderStatus.Cancelled)
                throw new ArgumentException(
                    $"No se pueden modificar las lineas de un pedido en estado {status}");
        }
    }
}
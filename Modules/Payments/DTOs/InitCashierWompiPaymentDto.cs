namespace SaborExpress.Modules.Payments.DTOs
{
    // El cajero solo manda el pedido. El monto lo calcula el back (total - lo ya pagado).
    public class InitCashierWompiPaymentDto
    {
        public int OrderId { get; set; }
    }
}
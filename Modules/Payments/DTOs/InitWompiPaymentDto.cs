// Modules/Payments/DTOs/InitWompiPaymentDto.cs
namespace SaborExpress.Modules.Payments.DTOs
{
    // Lo que manda el cliente desde el checkout para iniciar el pago
    public class InitWompiPaymentDto
    {
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
    }
}
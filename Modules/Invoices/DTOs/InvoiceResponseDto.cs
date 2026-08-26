// Modules/Invoices/DTOs/InvoiceResponseDto.cs
namespace SaborExpress.Modules.Invoices.DTOs
{
    public class InvoiceResponseDto
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public int PaymentId { get; set; }
        public int OrderId { get; set; }
        public int BranchId { get; set; }
        public string? BranchName { get; set; }
        public string? CustomerName { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
        public bool IsVoided { get; set; }
        public DateTime IssuedAt { get; set; }
    }
}

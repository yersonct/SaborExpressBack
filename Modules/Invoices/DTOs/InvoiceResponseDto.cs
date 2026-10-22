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
        public string? BranchAddress { get; set; }
        public string? BranchPhone { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerDocument { get; set; }
        public string? CustomerEmail { get; set; }
        public string? DeliveryAddressLine { get; set; }
        public string? DeliveryAddressReference { get; set; }
        public string? OrderType { get; set; }
        public int? TableNumber { get; set; }
        public string? CashierName { get; set; }
        public string? PaymentMethod { get; set; }
        public decimal? PaymentAmount { get; set; }
        public DateTime? PaidAt { get; set; }
        public List<InvoiceItemResponseDto> Items { get; set; } = new();
        public decimal SubTotal { get; set; }
        public decimal Tax { get; set; }
        public decimal TaxRate { get; set; }
        public bool TaxIncludedInPrice { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal Total { get; set; }
        public bool IsVoided { get; set; }
        public DateTime IssuedAt { get; set; }
    }

    public class InvoiceItemResponseDto
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total { get; set; }
        public string? Notes { get; set; }
        public bool IsToGo { get; set; }
    }
}

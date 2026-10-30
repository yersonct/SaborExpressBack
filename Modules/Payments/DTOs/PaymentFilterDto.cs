// Modules/Payments/DTOs/PaymentFilterDto.cs
namespace SaborExpress.Modules.Payments.DTOs
{
    // Filtros para GET /api/Payments (reportes de caja: Gerente/Admin)
    public class PaymentFilterDto : SaborExpress.Shared.Responses.PageQuery
    {
        public int? BranchId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
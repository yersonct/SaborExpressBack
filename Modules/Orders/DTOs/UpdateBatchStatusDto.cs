using SaborExpress.Modules.Orders.Enum;

namespace SaborExpress.Modules.Orders.DTOs
{
    public class UpdateBatchStatusDto
    {
        public OrderDetailStatus Status { get; set; }
    }
}
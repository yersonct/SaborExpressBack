// Modules/Deliveries/Interfaces/IDeliveryAssignmentService.cs
namespace SaborExpress.Modules.Deliveries.Interfaces
{
    public interface IDeliveryAssignmentService
    {
        Task AssignAutomaticallyAsync(int orderId);
        Task AssignPendingForBranchAsync(int branchId);
    }
}
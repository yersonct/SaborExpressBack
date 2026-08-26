namespace SaborExpress.Modules.Notifications.Enum
{
    public enum NotificationType
    {
        OrderCreated,
        OrderStatusChanged,
        OrderReady,
        DeliveryAssigned,
        DeliveryInTransit,
        DeliveryCompleted,
        ShiftEndingSoon,  
        PaymentConfirmed,
        Manual,
        System
    }
}
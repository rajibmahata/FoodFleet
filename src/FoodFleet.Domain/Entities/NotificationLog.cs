using FoodFleet.Domain.Common;
using FoodFleet.Domain.Enums;

namespace FoodFleet.Domain.Entities;

public class NotificationLog : BaseEntity
{
    public Guid OrderId { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public string EventType { get; private set; } = default!;
    public NotificationStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime SentAt { get; private set; } = DateTime.UtcNow;

    public Order Order { get; private set; } = default!;

    protected NotificationLog() { }

    public static NotificationLog Create(Guid orderId, NotificationChannel channel, string eventType, NotificationStatus status, string? errorMessage = null)
    {
        return new NotificationLog
        {
            OrderId = orderId,
            Channel = channel,
            EventType = eventType,
            Status = status,
            ErrorMessage = errorMessage
        };
    }
}

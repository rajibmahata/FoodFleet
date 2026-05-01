namespace FoodFleet.Domain.Enums;

public enum OrderStatus
{
    PendingPayment = 1,
    Confirmed = 2,
    Preparing = 3,
    OutForDelivery = 4,
    Delivered = 5,
    Cancelled = 6
}

public enum PaymentMethod
{
    UPI = 1,
    Razorpay = 2,
    Stripe = 3,
    COD = 4
}

public enum PaymentStatus
{
    Pending = 1,
    Completed = 2,
    Failed = 3,
    Refunded = 4
}

public enum NotificationChannel
{
    Email = 1,
    SMS = 2,
    Push = 3
}

public enum NotificationStatus
{
    Sent = 1,
    Failed = 2,
    Skipped = 3
}

using FoodFleet.Domain.Entities;
using FoodFleet.Domain.Enums;
using FoodFleet.Domain.Interfaces.Services;
using FoodFleet.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace FoodFleet.Infrastructure.Services;

/// <summary>Composite notification service — dispatches to email + SMS based on RestaurantSettings toggles.</summary>
public class NotificationService(IUnitOfWork uow, IEmailService emailService, ISmsService smsService, ILogger<NotificationService> logger)
    : INotificationService
{
    public Task SendOrderConfirmedAsync(Order order, CancellationToken ct = default) =>
        DispatchAsync(order, "OrderPlaced",
            $"Order #{order.Id} Confirmed", $"Your order #{order.Id} has been confirmed and will be prepared shortly.", ct);

    public Task SendOrderPreparingAsync(Order order, CancellationToken ct = default) =>
        DispatchAsync(order, "OrderPreparing",
            $"Order #{order.Id} Being Prepared", $"Your order #{order.Id} is being prepared.", ct);

    public Task SendOutForDeliveryAsync(Order order, DeliveryPartner partner, CancellationToken ct = default) =>
        DispatchAsync(order, "OutForDelivery",
            $"Order #{order.Id} Out for Delivery",
            $"Your order is on the way! Your delivery partner is {partner.Name}, reachable at {partner.Phone}.", ct);

    public Task SendOrderDeliveredAsync(Order order, CancellationToken ct = default) =>
        DispatchAsync(order, "OrderDelivered",
            $"Order #{order.Id} Delivered", $"Your order #{order.Id} has been delivered. Enjoy your meal!", ct);

    public Task SendOrderCancelledAsync(Order order, CancellationToken ct = default) =>
        DispatchAsync(order, "OrderCancelled",
            $"Order #{order.Id} Cancelled",
            $"Your order #{order.Id} has been cancelled. Reason: {order.CancellationReason}", ct);

    private async Task DispatchAsync(Order order, string eventType, string subject, string message, CancellationToken ct)
    {
        var tasks = new List<Task<bool>>
        {
            TrySendEmailAsync(order, subject, message, ct),
            TrySendSmsAsync(order, message, ct)
        };

        var results = await Task.WhenAll(tasks);

        // Log notification results
        foreach (var (channel, success) in new[] { (NotificationChannel.Email, results[0]), (NotificationChannel.SMS, results[1]) })
        {
            var log = NotificationLog.Create(order.Id, channel, eventType,
                success ? NotificationStatus.Sent : NotificationStatus.Skipped);
            await uow.NotificationLogs.AddAsync(log, ct);
        }

        try { await uow.SaveChangesAsync(ct); }
        catch (Exception ex) { logger.LogWarning(ex, "Failed to save notification logs for order {OrderId}", order.Id); }
    }

    private async Task<bool> TrySendEmailAsync(Order order, string subject, string body, CancellationToken ct)
    {
        try
        {
            if (order.Customer?.Email is null) return false;
            await emailService.SendAsync(order.Customer.Email, subject, body, ct);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Email notification failed for order {OrderId}", order.Id);
            return false;
        }
    }

    private async Task<bool> TrySendSmsAsync(Order order, string message, CancellationToken ct)
    {
        try
        {
            if (order.Customer?.Phone is null) return false;
            await smsService.SendAsync(order.Customer.Phone, message, ct);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "SMS notification failed for order {OrderId}", order.Id);
            return false;
        }
    }
}

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct = default);
}

public interface ISmsService
{
    Task SendAsync(string to, string message, CancellationToken ct = default);
}

/// <summary>SMTP email service — configurable via RestaurantSettings.</summary>
public class SmtpEmailService(ISettingsRepository settings, ILogger<SmtpEmailService> logger) : IEmailService
{
    public async Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        // Simplified — in production use MailKit/SendGrid
        logger.LogInformation("Email queued to {To}: {Subject}", to, subject);
        await Task.CompletedTask;
    }
}

/// <summary>SMS stub — configure fast2SMS or Twilio keys in settings.</summary>
public class SmsService(ISettingsRepository settings, ILogger<SmsService> logger) : ISmsService
{
    public async Task SendAsync(string to, string message, CancellationToken ct = default)
    {
        logger.LogInformation("SMS queued to {To}: {Message}", to, message);
        await Task.CompletedTask;
    }
}

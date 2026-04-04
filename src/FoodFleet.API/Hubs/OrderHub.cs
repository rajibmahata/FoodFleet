using FoodFleet.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FoodFleet.API.Hubs;

/// <summary>Real-time order feed for admin panel. Clients subscribe per-branch or restaurant-wide.</summary>
[Authorize(Roles = "Admin,SuperAdmin")]
public class OrderHub : Hub
{
    public const string Endpoint = "/hubs/orders";

    /// <summary>Admin client calls this after connecting to join a branch-specific group.</summary>
    public async Task SubscribeToBranch(Guid branchId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, BranchGroup(branchId));
    }

    public async Task UnsubscribeFromBranch(Guid branchId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, BranchGroup(branchId));
    }

    public static string BranchGroup(Guid branchId) => $"branch-{branchId}";
    public static string OrderGroup(Guid orderId) => $"order-{orderId}";
}

/// <summary>Typed client interface for strongly-typed SignalR push.</summary>
public interface IOrderHubClient
{
    Task NewOrderReceived(OrderNotificationPayload payload);
    Task OrderStatusUpdated(Guid orderId, string newStatus);
}

public record OrderNotificationPayload(
    Guid OrderId,
    Guid BranchId,
    string CustomerName,
    decimal TotalAmount,
    string PaymentMethod,
    DateTime PlacedAt);

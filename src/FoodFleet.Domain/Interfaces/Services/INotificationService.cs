using FoodFleet.Domain.Entities;
using FoodFleet.Domain.Enums;

namespace FoodFleet.Domain.Interfaces.Services;

public interface INotificationService
{
    Task SendOrderConfirmedAsync(Order order, CancellationToken ct = default);
    Task SendOrderPreparingAsync(Order order, CancellationToken ct = default);
    Task SendOutForDeliveryAsync(Order order, DeliveryPartner partner, CancellationToken ct = default);
    Task SendOrderDeliveredAsync(Order order, CancellationToken ct = default);
    Task SendOrderCancelledAsync(Order order, CancellationToken ct = default);
}

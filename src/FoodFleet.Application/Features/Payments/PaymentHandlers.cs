using FoodFleet.Application.Common;
using FoodFleet.Application.DTOs;
using FoodFleet.Domain.Entities;
using FoodFleet.Domain.Interfaces.Repositories;
using FoodFleet.Domain.Interfaces.Services;
using MediatR;

namespace FoodFleet.Application.Features.Payments;

public record InitiatePaymentCommand(Guid CustomerId, InitiatePaymentRequest Request) : IRequest<Result<PaymentInitResponse>>;
public record ConfirmPaymentCommand(Guid CustomerId, ConfirmPaymentRequest Request) : IRequest<Result<PaymentDto>>;
public record HandleWebhookCommand(string Gateway, string Payload, string Signature) : IRequest<Result>;

public class InitiatePaymentCommandHandler(IUnitOfWork uow, IEnumerable<IPaymentGateway> gateways)
    : IRequestHandler<InitiatePaymentCommand, Result<PaymentInitResponse>>
{
    public async Task<Result<PaymentInitResponse>> Handle(InitiatePaymentCommand cmd, CancellationToken ct)
    {
        var order = await uow.Orders.GetWithDetailsAsync(cmd.Request.OrderId, ct);
        if (order is null) return Result<PaymentInitResponse>.NotFound("Order not found.");
        if (order.CustomerId != cmd.CustomerId) return Result<PaymentInitResponse>.Forbidden();

        var gateway = gateways.FirstOrDefault(g => g.GatewayName.Equals(cmd.Request.Gateway, StringComparison.OrdinalIgnoreCase));
        if (gateway is null) return Result<PaymentInitResponse>.Failure($"Payment gateway '{cmd.Request.Gateway}' not available.");

        var initResult = await gateway.InitiateAsync(order, ct);
        var payment = await uow.Payments.GetByOrderAsync(order.Id, ct)
                      ?? Payment.Create(order.Id, cmd.Request.Gateway, order.TotalAmount);

        payment.SetGatewayRef(initResult.SessionToken);
        uow.Payments.Update(payment);
        await uow.SaveChangesAsync(ct);

        return Result<PaymentInitResponse>.Success(new PaymentInitResponse(initResult.SessionToken, initResult.RedirectUrl, initResult.IsImmediate, payment.Id));
    }
}

public class ConfirmPaymentCommandHandler(IUnitOfWork uow, IEnumerable<IPaymentGateway> gateways)
    : IRequestHandler<ConfirmPaymentCommand, Result<PaymentDto>>
{
    public async Task<Result<PaymentDto>> Handle(ConfirmPaymentCommand cmd, CancellationToken ct)
    {
        var order = await uow.Orders.GetWithDetailsAsync(cmd.Request.OrderId, ct);
        if (order is null) return Result<PaymentDto>.NotFound("Order not found.");
        if (order.CustomerId != cmd.CustomerId) return Result<PaymentDto>.Forbidden();

        var payment = await uow.Payments.GetByOrderAsync(order.Id, ct);
        if (payment is null) return Result<PaymentDto>.NotFound("Payment record not found.");

        var gateway = gateways.FirstOrDefault(g => g.GatewayName.Equals(payment.Gateway, StringComparison.OrdinalIgnoreCase));
        if (gateway is null) return Result<PaymentDto>.Failure("Gateway not available.");

        var confirmResult = await gateway.ConfirmAsync(cmd.Request.GatewayRefId, order.TotalAmount, ct);
        if (!confirmResult.Success)
        {
            payment.MarkFailed();
            uow.Payments.Update(payment);
            await uow.SaveChangesAsync(ct);
            return Result<PaymentDto>.Failure(confirmResult.ErrorMessage ?? "Payment confirmation failed.");
        }

        payment.MarkCompleted(confirmResult.GatewayRefId!);
        order.UpdateStatus(Domain.Enums.OrderStatus.Confirmed);
        uow.Payments.Update(payment);
        uow.Orders.Update(order);
        await uow.SaveChangesAsync(ct);

        return Result<PaymentDto>.Success(PaymentMapper.MapPaymentDto(payment));
    }
}

internal static class PaymentMapper
{
    internal static PaymentDto MapPaymentDto(Payment p) =>
        new(p.Id, p.OrderId, p.Gateway, p.GatewayRefId, p.Status.ToString(), p.Amount, p.PaidAt);
}

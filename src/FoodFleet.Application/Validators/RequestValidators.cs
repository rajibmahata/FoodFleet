using FluentValidation;
using FoodFleet.Application.DTOs;

namespace FoodFleet.Application.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Phone).NotEmpty().Matches(@"^\+?[\d\s\-]{7,20}$").WithMessage("Invalid phone number format.");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class CreateBranchRequestValidator : AbstractValidator<CreateBranchRequest>
{
    public CreateBranchRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Lat).InclusiveBetween(-90, 90);
        RuleFor(x => x.Lng).InclusiveBetween(-180, 180);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
        RuleFor(x => x.DeliveryRadiusKm).GreaterThan(0).LessThanOrEqualTo(50);
    }
}

public class ValidateDeliveryRequestValidator : AbstractValidator<ValidateDeliveryRequest>
{
    public ValidateDeliveryRequestValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.DeliveryLat).InclusiveBetween(-90, 90);
        RuleFor(x => x.DeliveryLng).InclusiveBetween(-180, 180);
    }
}

public class PlaceOrderRequestValidator : AbstractValidator<PlaceOrderRequest>
{
    private static readonly string[] ValidPaymentMethods = { "UPI", "Razorpay", "Stripe", "COD" };

    public PlaceOrderRequestValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.PaymentMethod).NotEmpty()
            .Must(m => ValidPaymentMethods.Contains(m, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Payment method must be one of: {string.Join(", ", ValidPaymentMethods)}");
        RuleFor(x => x.DeliveryAddress).NotEmpty().MaximumLength(500);
        RuleFor(x => x.DeliveryLat).InclusiveBetween(-90, 90);
        RuleFor(x => x.DeliveryLng).InclusiveBetween(-180, 180);
        RuleFor(x => x.Items).NotEmpty().WithMessage("Order must have at least one item.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.MenuItemId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0).LessThanOrEqualTo(50);
        });
    }
}

public class CreateMenuItemRequestValidator : AbstractValidator<CreateMenuItemRequest>
{
    public CreateMenuItemRequestValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
        RuleFor(x => x.StockCount).GreaterThanOrEqualTo(0).When(x => x.StockCount.HasValue);
    }
}

public class CreateAddressRequestValidator : AbstractValidator<CreateAddressRequest>
{
    public CreateAddressRequestValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(50);
        RuleFor(x => x.FullAddress).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Lat).InclusiveBetween(-90, 90);
        RuleFor(x => x.Lng).InclusiveBetween(-180, 180);
    }
}

public class CreateDeliveryPartnerRequestValidator : AbstractValidator<CreateDeliveryPartnerRequest>
{
    public CreateDeliveryPartnerRequestValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).NotEmpty().Matches(@"^\+[\d]{1,3}[\-\s]?[\d]{6,14}$")
            .WithMessage("Phone must include country code e.g. +91-9876543210");
    }
}

namespace FoodFleet.Application.DTOs;

// ── Auth ─────────────────────────────────────────────
public record RegisterRequest(string Name, string Email, string Phone, string Password);
public record LoginRequest(string Email, string Password);
public record RefreshTokenRequest(string RefreshToken);
public record AuthResponse(string AccessToken, string RefreshToken, DateTime AccessTokenExpiry, CustomerDto Customer);
public record AdminLoginRequest(string Username, string Password);
public record AdminAuthResponse(string AccessToken, string RefreshToken, DateTime AccessTokenExpiry, string Username, IEnumerable<string> Roles);

// ── Restaurant ───────────────────────────────────────
public record RestaurantInfoDto(Guid Id, string Name, string? LogoUrl, string ContactEmail);
public record UpdateRestaurantRequest(string Name, string ContactEmail, string? LogoUrl);

// ── Branch ───────────────────────────────────────────
public record BranchDto(Guid Id, Guid RestaurantId, string Name, double Lat, double Lng, string Address, double DeliveryRadiusKm, bool IsActive);
public record CreateBranchRequest(string Name, double Lat, double Lng, string Address, double DeliveryRadiusKm = 3.0);
public record UpdateBranchRequest(string Name, double Lat, double Lng, string Address, double DeliveryRadiusKm);
public record UpdateDeliveryRadiusRequest(double RadiusKm);
public record NearestBranchRequest(double Lat, double Lng);

// ── Geo ──────────────────────────────────────────────
public record ValidateDeliveryRequest(Guid BranchId, double DeliveryLat, double DeliveryLng);
public record GeoValidationResponse(bool IsDeliverable, double DistanceKm, double RadiusKm);

// ── Menu ─────────────────────────────────────────────
public record MenuCategoryDto(Guid Id, Guid BranchId, string Name, int DisplayOrder, bool IsActive, IEnumerable<MenuItemDto> Items);
public record MenuItemDto(Guid Id, Guid CategoryId, string Name, string? Description, decimal Price, string? ImageUrl, bool IsAvailable, int? StockCount, IEnumerable<MenuItemVariantDto> Variants);
public record MenuItemVariantDto(Guid Id, string Label, decimal AdditionalPrice);
public record CreateMenuCategoryRequest(Guid BranchId, string Name, int DisplayOrder = 0);
public record UpdateMenuCategoryRequest(string Name, int DisplayOrder);
public record CreateMenuItemRequest(Guid CategoryId, string Name, decimal Price, string? Description = null, string? ImageUrl = null, int? StockCount = null);
public record UpdateMenuItemRequest(string Name, string? Description, decimal Price, string? ImageUrl);
public record UpdateStockRequest(int? StockCount);
public record UpdateAvailabilityRequest(bool IsAvailable);
public record CreateMenuItemVariantRequest(Guid MenuItemId, string Label, decimal AdditionalPrice = 0);
public record BranchMenuResponse(BranchDto Branch, IEnumerable<MenuCategoryDto> Categories);

// ── Customer ─────────────────────────────────────────
public record CustomerDto(Guid Id, string Name, string Email, string Phone, DateTime CreatedAt);
public record UpdateProfileRequest(string Name, string Phone);
public record CreateAddressRequest(string Label, string FullAddress, double Lat, double Lng, bool IsDefault = false);
public record CustomerAddressDto(Guid Id, string Label, string FullAddress, double Lat, double Lng, bool IsDefault);

// ── Order ────────────────────────────────────────────
public record PlaceOrderRequest(
    Guid BranchId,
    string PaymentMethod,
    string DeliveryAddress,
    double DeliveryLat,
    double DeliveryLng,
    List<OrderItemRequest> Items
);
public record OrderItemRequest(Guid MenuItemId, Guid? VariantId, int Quantity);
public record OrderDto(
    Guid Id, Guid CustomerId, Guid BranchId, string Status, decimal TotalAmount,
    string PaymentMethod, string DeliveryAddress, DateTime PlacedAt,
    IEnumerable<OrderItemDto> Items, DeliveryPartnerContactDto? DeliveryPartner
);
public record OrderItemDto(Guid Id, Guid MenuItemId, string ItemName, Guid? VariantId, string? VariantLabel, int Quantity, decimal UnitPrice, decimal TotalPrice);
public record DeliveryPartnerContactDto(string Name, string Phone);
public record UpdateOrderStatusRequest(string Status);
public record CancelOrderRequest(string Reason);
public record AssignDeliveryPartnerRequest(Guid DeliveryPartnerId);

// ── Payment ──────────────────────────────────────────
public record InitiatePaymentRequest(Guid OrderId, string Gateway);
public record PaymentInitResponse(string SessionToken, string? RedirectUrl, bool IsImmediate, Guid PaymentId);
public record ConfirmPaymentRequest(Guid OrderId, string GatewayRefId);
public record PaymentDto(Guid Id, Guid OrderId, string Gateway, string? GatewayRefId, string Status, decimal Amount, DateTime? PaidAt);

// ── Delivery Partners ────────────────────────────────
public record DeliveryPartnerDto(Guid Id, Guid BranchId, string Name, string Phone, bool IsAvailable);
public record CreateDeliveryPartnerRequest(Guid BranchId, string Name, string Phone);
public record UpdateDeliveryPartnerRequest(string Name, string Phone);

// ── Settings ─────────────────────────────────────────
public record SettingDto(string Key, string Value);
public record UpsertSettingRequest(string Key, string Value);
public record BulkUpsertSettingsRequest(List<UpsertSettingRequest> Settings);

// ── Excel Import ─────────────────────────────────────
public record MenuImportResponse(int Imported, int Skipped, List<RowErrorDto> Errors);
public record RowErrorDto(int Row, string Reason);

// ── Analytics ────────────────────────────────────────
public record DashboardSummaryDto(
    int TodaysOrders, decimal TodaysRevenue, int PendingOrders,
    int ActiveBranches, int LowStockItems
);
public record RevenueReportDto(DateTime Date, string Branch, string Gateway, decimal Amount, int OrderCount);

public record ExcelImportResultDto(int Imported, int Skipped, List<string> Errors);

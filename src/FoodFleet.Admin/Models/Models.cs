namespace FoodFleet.Admin.Models;

// ── Auth ──────────────────────────────────────────────────────

public record AdminLoginRequest(string Email, string Password);
public record AuthResponse(string AccessToken, string RefreshToken, DateTime AccessTokenExpiry);

// ── Branches ─────────────────────────────────────────────────

public record BranchDto(
    Guid Id, string Name, string Address, string City, string PhoneNumber,
    double Latitude, double Longitude, string OpeningTime, string ClosingTime,
    double DeliveryRadiusKm, bool IsActive, DateTime CreatedAt);

public record CreateBranchRequest(
    string Name, string Address, string City, string PhoneNumber,
    double Latitude, double Longitude, string OpeningTime, string ClosingTime,
    double DeliveryRadiusKm);

public record UpdateBranchRequest(
    string Name, string Address, string City, string PhoneNumber,
    double Latitude, double Longitude, string OpeningTime, string ClosingTime,
    double DeliveryRadiusKm, bool IsActive);

// ── Menu ─────────────────────────────────────────────────────

public record MenuCategoryDto(Guid Id, Guid BranchId, string Name, int SortOrder);

public record MenuItemDto(
    Guid Id, Guid CategoryId, string Name, string? Description, decimal Price,
    string? ImageUrl, bool IsAvailable, int? StockCount,
    int PreparationTimeMinutes, bool IsVegetarian, bool IsVegan,
    bool IsGlutenFree, int SpiceLevel);

public record CreateMenuCategoryRequest(Guid BranchId, string Name, int SortOrder = 0);
public record UpdateMenuCategoryRequest(string Name, int SortOrder);

public record CreateMenuItemRequest(
    Guid CategoryId, string Name, string? Description, decimal Price,
    int PreparationTimeMinutes, int? StockCount,
    bool IsVegetarian, bool IsVegan, bool IsGlutenFree, int SpiceLevel);

public record UpdateMenuItemRequest(
    string Name, string? Description, decimal Price,
    int PreparationTimeMinutes, bool IsVegetarian,
    bool IsVegan, bool IsGlutenFree, int SpiceLevel);

public record UpdateStockRequest(int? StockCount);
public record UpdateAvailabilityRequest(bool IsAvailable);
public record ExcelImportResultDto(int Imported, int Skipped, List<string> Errors);

// ── Orders ───────────────────────────────────────────────────

public class OrderDto
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public Guid BranchId { get; init; }
    public string BranchName { get; init; } = string.Empty;
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public string DeliveryAddress { get; init; } = string.Empty;
    public DateTime PlacedAt { get; init; }
    public Guid? DeliveryPartnerId { get; init; }
    public string? DeliveryPartnerName { get; init; }
    public List<OrderItemDto> Items { get; init; } = new();
}

public record OrderItemDto(
    Guid Id, Guid MenuItemId, string MenuItemName,
    int Quantity, decimal UnitPrice, decimal TotalPrice);

public record UpdateOrderStatusRequest(string Status);
public record AssignDeliveryPartnerRequest(Guid DeliveryPartnerId);

// ── Delivery Partners ─────────────────────────────────────────

public class DeliveryPartnerDto
{
    public Guid Id { get; init; }
    public Guid BranchId { get; init; }
    public string BranchName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string VehicleType { get; init; } = string.Empty;
    public bool IsAvailable { get; set; }
    public int TotalOrdersCompleted { get; init; }
    public double? AverageRating { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateDeliveryPartnerRequest(Guid BranchId, string Name, string PhoneNumber, string? Email, string VehicleType);

// ── Settings ─────────────────────────────────────────────────

public class SettingDto
{
    public Guid Id { get; init; }
    public string Key { get; init; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; init; }
}

public record SettingUpsertItem(string Key, string Value);
public record UpsertSettingRequest(string Key, string Value);
public record BulkUpsertSettingsRequest(List<UpsertSettingRequest> Settings);

// ── Analytics ────────────────────────────────────────────────

public class DashboardSummaryDto
{
    public int TodaysOrders { get; init; }
    public decimal TodaysRevenue { get; init; }
    public int PendingOrders { get; init; }
    public int ActiveBranches { get; init; }
    public int LowStockItems { get; init; }
    public double AvgPrepTimeMinutes { get; init; }
    public List<RecentOrderSummary> RecentOrders { get; init; } = new();
    public List<TopMenuItemSummary> TopMenuItems { get; init; } = new();
}

public record RecentOrderSummary(string OrderNumber, string BranchName, string Status, decimal TotalAmount);
public record TopMenuItemSummary(string Name, int OrderCount, decimal Revenue);

// ── Generic API result ────────────────────────────────────────

public class ApiResult<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
    public int StatusCode { get; init; }
}

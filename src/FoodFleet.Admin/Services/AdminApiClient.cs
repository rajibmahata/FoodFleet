using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blazored.LocalStorage;
using FoodFleet.Admin.Models;

namespace FoodFleet.Admin.Services;

public class AdminApiClient(HttpClient http, ILocalStorageService localStorage)
{
    private const string TokenKey = "admin_access_token";

    // ── Auth helpers ──────────────────────────────────────────

    public async Task SetTokenAsync(string token)
    {
        await localStorage.SetItemAsStringAsync(TokenKey, token);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<string?> GetTokenAsync()
        => await localStorage.GetItemAsStringAsync(TokenKey);

    public async Task ClearTokenAsync()
    {
        await localStorage.RemoveItemAsync(TokenKey);
        http.DefaultRequestHeaders.Authorization = null;
    }

    public async Task EnsureAuthHeaderAsync()
    {
        if (http.DefaultRequestHeaders.Authorization is null)
        {
            string? token;
            try { token = await GetTokenAsync(); }
            catch (InvalidOperationException) { return; } // JS interop not available (SSR prerender)
            if (!string.IsNullOrEmpty(token))
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    // ── Auth ──────────────────────────────────────────────────

    public async Task<ApiResult<AuthResponse>> LoginAsync(AdminLoginRequest request)
    {
        var response = await http.PostAsJsonAsync("api/auth/admin/login", request);
        return await ParseAsync<AuthResponse>(response);
    }

    // ── Analytics ─────────────────────────────────────────────

    public async Task<ApiResult<DashboardSummaryDto>> GetDashboardAsync(Guid? branchId = null)
    {
        await EnsureAuthHeaderAsync();
        var url = branchId.HasValue
            ? $"api/admin/analytics/dashboard?branchId={branchId}"
            : "api/admin/analytics/dashboard";
        return await GetAsync<DashboardSummaryDto>(url);
    }

    // ── Branches ─────────────────────────────────────────────

    public async Task<ApiResult<List<BranchDto>>> GetBranchesAsync()
    {
        await EnsureAuthHeaderAsync();
        return await GetAsync<List<BranchDto>>("api/admin/branches");
    }

    public async Task<ApiResult<BranchDto>> CreateBranchAsync(CreateBranchRequest request)
    {
        await EnsureAuthHeaderAsync();
        return await PostAsync<BranchDto>("api/admin/branches", request);
    }

    public async Task<ApiResult<BranchDto>> UpdateBranchAsync(Guid id, UpdateBranchRequest request)
    {
        await EnsureAuthHeaderAsync();
        return await PutAsync<BranchDto>($"api/admin/branches/{id}", request);
    }

    public async Task<ApiResult<object>> DeleteBranchAsync(Guid id)
    {
        await EnsureAuthHeaderAsync();
        var response = await http.DeleteAsync($"api/admin/branches/{id}");
        return new ApiResult<object> { IsSuccess = response.IsSuccessStatusCode, StatusCode = (int)response.StatusCode };
    }

    // ── Menu Categories ───────────────────────────────────────

    public async Task<ApiResult<List<MenuCategoryDto>>> GetMenuCategoriesAsync(Guid branchId)
    {
        await EnsureAuthHeaderAsync();
        return await GetAsync<List<MenuCategoryDto>>($"api/admin/menu/categories/{branchId}");
    }

    public async Task<ApiResult<List<MenuItemDto>>> GetMenuItemsAsync(Guid categoryId)
    {
        await EnsureAuthHeaderAsync();
        return await GetAsync<List<MenuItemDto>>($"api/admin/menu/items?categoryId={categoryId}");
    }

    public async Task<ApiResult<MenuCategoryDto>> CreateMenuCategoryAsync(CreateMenuCategoryRequest request)
    {
        await EnsureAuthHeaderAsync();
        return await PostAsync<MenuCategoryDto>("api/admin/menu/categories", request);
    }

    public async Task<ApiResult<MenuCategoryDto>> UpdateMenuCategoryAsync(Guid id, UpdateMenuCategoryRequest request)
    {
        await EnsureAuthHeaderAsync();
        return await PutAsync<MenuCategoryDto>($"api/admin/menu/categories/{id}", request);
    }

    public async Task<ApiResult<object>> DeleteMenuCategoryAsync(Guid id)
    {
        await EnsureAuthHeaderAsync();
        var response = await http.DeleteAsync($"api/admin/menu/categories/{id}");
        return new ApiResult<object> { IsSuccess = response.IsSuccessStatusCode, StatusCode = (int)response.StatusCode };
    }

    // ── Menu Items ────────────────────────────────────────────

    public async Task<ApiResult<MenuItemDto>> CreateMenuItemAsync(CreateMenuItemRequest request)
    {
        await EnsureAuthHeaderAsync();
        return await PostAsync<MenuItemDto>("api/admin/menu/items", request);
    }

    public async Task<ApiResult<MenuItemDto>> UpdateMenuItemAsync(Guid id, UpdateMenuItemRequest request)
    {
        await EnsureAuthHeaderAsync();
        return await PutAsync<MenuItemDto>($"api/admin/menu/items/{id}", request);
    }

    public async Task<ApiResult<object>> DeleteMenuItemAsync(Guid id)
    {
        await EnsureAuthHeaderAsync();
        var response = await http.DeleteAsync($"api/admin/menu/items/{id}");
        return new ApiResult<object> { IsSuccess = response.IsSuccessStatusCode, StatusCode = (int)response.StatusCode };
    }

    public async Task<ApiResult<MenuItemDto>> UpdateStockAsync(Guid itemId, int? stockCount)
    {
        await EnsureAuthHeaderAsync();
        return await PatchAsync<MenuItemDto>($"api/admin/menu/items/{itemId}/stock", new UpdateStockRequest(stockCount));
    }

    public async Task<ApiResult<MenuItemDto>> SetAvailabilityAsync(Guid itemId, bool isAvailable)
    {
        await EnsureAuthHeaderAsync();
        return await PatchAsync<MenuItemDto>($"api/admin/menu/items/{itemId}/availability", new UpdateAvailabilityRequest(isAvailable));
    }

    public async Task<ApiResult<object>> ExportMenuExcelAsync(Guid branchId)
    {
        await EnsureAuthHeaderAsync();
        var response = await http.GetAsync($"api/admin/menu/export/{branchId}");
        return new ApiResult<object> { IsSuccess = response.IsSuccessStatusCode, StatusCode = (int)response.StatusCode,
            Error = response.IsSuccessStatusCode ? null : "Export failed" };
    }

    public async Task<ApiResult<ExcelImportResultDto>> ImportMenuExcelAsync(Guid branchId, Stream fileStream, string fileName)
    {
        await EnsureAuthHeaderAsync();
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);
        content.Add(streamContent, "file", fileName);
        var response = await http.PostAsync($"api/admin/menu/import/{branchId}", content);
        return await ParseAsync<ExcelImportResultDto>(response);
    }

    // ── Orders ────────────────────────────────────────────────

    public async Task<ApiResult<List<OrderDto>>> GetOrdersAsync(
        Guid? branchId = null, string? status = null,
        DateTime? from = null, DateTime? to = null)
    {
        await EnsureAuthHeaderAsync();
        var query = BuildQueryString(
            ("branchId", branchId?.ToString()),
            ("status", status),
            ("from", from?.ToString("yyyy-MM-dd")),
            ("to", to?.ToString("yyyy-MM-dd")));
        return await GetAsync<List<OrderDto>>($"api/admin/orders{query}");
    }

    public async Task<ApiResult<OrderDto>> UpdateOrderStatusAsync(Guid orderId, string status)
    {
        await EnsureAuthHeaderAsync();
        return await PatchAsync<OrderDto>($"api/admin/orders/{orderId}/status", new UpdateOrderStatusRequest(status));
    }

    public async Task<ApiResult<OrderDto>> AssignDeliveryPartnerAsync(Guid orderId, Guid partnerId)
    {
        await EnsureAuthHeaderAsync();
        return await PatchAsync<OrderDto>($"api/admin/orders/{orderId}/assign-partner", new AssignDeliveryPartnerRequest(partnerId));
    }

    // ── Delivery Partners ─────────────────────────────────────

    public async Task<ApiResult<List<DeliveryPartnerDto>>> GetDeliveryPartnersAsync(Guid? branchId = null)
    {
        await EnsureAuthHeaderAsync();
        var url = branchId.HasValue
            ? $"api/admin/delivery-partners?branchId={branchId}"
            : "api/admin/delivery-partners";
        return await GetAsync<List<DeliveryPartnerDto>>(url);
    }

    public async Task<ApiResult<DeliveryPartnerDto>> CreateDeliveryPartnerAsync(CreateDeliveryPartnerRequest request)
    {
        await EnsureAuthHeaderAsync();
        return await PostAsync<DeliveryPartnerDto>("api/admin/delivery-partners", request);
    }

    public async Task<ApiResult<DeliveryPartnerDto>> ToggleDeliveryPartnerAvailabilityAsync(Guid id, bool isAvailable)
    {
        await EnsureAuthHeaderAsync();
        return await PatchAsync<DeliveryPartnerDto>($"api/admin/delivery-partners/{id}/availability",
            new UpdateAvailabilityRequest(isAvailable));
    }

    // ── Settings ─────────────────────────────────────────────

    public async Task<ApiResult<List<SettingDto>>> GetSettingsAsync()
    {
        await EnsureAuthHeaderAsync();
        return await GetAsync<List<SettingDto>>("api/admin/settings");
    }

    public async Task<ApiResult<object>> BulkUpsertSettingsAsync(List<SettingUpsertItem> settings)
    {
        await EnsureAuthHeaderAsync();
        var payload = new BulkUpsertSettingsRequest(settings.Select(s => new UpsertSettingRequest(s.Key, s.Value)).ToList());
        return await PutAsync<object>("api/admin/settings", payload);
    }

    // ── Private HTTP helpers ──────────────────────────────────

    private async Task<ApiResult<T>> GetAsync<T>(string url)
    {
        var response = await http.GetAsync(url);
        return await ParseAsync<T>(response);
    }

    private async Task<ApiResult<T>> PostAsync<T>(string url, object body)
    {
        var response = await http.PostAsJsonAsync(url, body);
        return await ParseAsync<T>(response);
    }

    private async Task<ApiResult<T>> PutAsync<T>(string url, object body)
    {
        var response = await http.PutAsJsonAsync(url, body);
        return await ParseAsync<T>(response);
    }

    private async Task<ApiResult<T>> PatchAsync<T>(string url, object body)
    {
        var response = await http.PatchAsJsonAsync(url, body);
        return await ParseAsync<T>(response);
    }

    private static async Task<ApiResult<T>> ParseAsync<T>(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            try
            {
                var data = await response.Content.ReadFromJsonAsync<T>();
                return new ApiResult<T> { IsSuccess = true, Data = data, StatusCode = (int)response.StatusCode };
            }
            catch
            {
                return new ApiResult<T> { IsSuccess = true, StatusCode = (int)response.StatusCode };
            }
        }

        string? error = null;
        try { error = await response.Content.ReadAsStringAsync(); } catch { }
        return new ApiResult<T> { IsSuccess = false, Error = error, StatusCode = (int)response.StatusCode };
    }

    private static string BuildQueryString(params (string key, string? value)[] pairs)
    {
        var parts = pairs
            .Where(p => p.value is not null)
            .Select(p => $"{Uri.EscapeDataString(p.key)}={Uri.EscapeDataString(p.value!)}");
        var qs = string.Join("&", parts);
        return qs.Length > 0 ? $"?{qs}" : string.Empty;
    }
}

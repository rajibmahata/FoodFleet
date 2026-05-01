using FoodFleet.Admin.Services;
using Microsoft.AspNetCore.Components;

namespace FoodFleet.Admin.Components;

/// <summary>
/// Base class for protected (authenticated) page components.
/// Redirects to /login after the interactive circuit connects if the user is not authenticated.
/// </summary>
public abstract class AuthenticatedComponentBase : ComponentBase
{
    [Inject] protected AdminAuthStateProvider AuthProvider { get; set; } = default!;
    [Inject] protected NavigationManager Nav { get; set; } = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        var authState = await AuthProvider.GetAuthenticationStateAsync();
        if (!(authState.User.Identity?.IsAuthenticated ?? false))
            Nav.NavigateTo("/login");
    }
}

using FoodFleet.Application.DTOs;
using FoodFleet.Application.Features.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodFleet.API.Controllers;

/// <summary>Admin analytics — dashboard KPIs and summary statistics.</summary>
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/admin/analytics")]
[Produces("application/json")]
public class AdminAnalyticsController : ApiControllerBase
{
    /// <summary>Dashboard summary: today's orders, revenue, low-stock items, top menu items.</summary>
    /// <param name="branchId">Optional. Filter stats to a specific branch. Omit for all branches.</param>
    /// <response code="200">Dashboard data returned.</response>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardSummaryDto), 200)]
    public async Task<IActionResult> Dashboard([FromQuery] Guid? branchId, CancellationToken ct)
        => ToActionResult(await Mediator.Send(new GetDashboardSummaryQuery(branchId), ct));
}

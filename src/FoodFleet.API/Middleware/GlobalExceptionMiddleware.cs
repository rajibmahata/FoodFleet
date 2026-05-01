using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace FoodFleet.API.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException vex)
        {
            logger.LogWarning("Validation failed: {Errors}", string.Join("; ", vex.Errors.Select(e => e.ErrorMessage)));
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";
            var errors = vex.Errors.GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { type = "ValidationError", errors }));
        }
        catch (UnauthorizedAccessException uex)
        {
            logger.LogWarning(uex, "Unauthorized access");
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { type = "Unauthorized", message = "Authentication required." }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            var isDev = context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment();
            var body = isDev
                ? new { type = "ServerError", message = ex.Message, detail = ex.StackTrace }
                : new { type = "ServerError", message = "An unexpected error occurred.", detail = (string?)null };
            await context.Response.WriteAsync(JsonSerializer.Serialize(body));
        }
    }
}

using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using FoodFleet.Application;
using FoodFleet.Infrastructure;
using FoodFleet.Infrastructure.Data;
using FoodFleet.API.Middleware;
using FoodFleet.API.Hubs;
using FoodFleet.API.HealthChecks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text.Json;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // --- Serilog ---
    builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

    // --- Application + Infrastructure ---
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // --- CORS ---
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontends", policy =>
            policy.WithOrigins(
                    "http://localhost:3000",   // React Native web
                    "http://localhost:5000",   // Blazor WASM
                    "http://localhost:5001",   // Admin Blazor
                    "https://localhost:7000")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()); // needed for SignalR
    });

    // --- Rate limiting (auth endpoints) ---
    builder.Services.AddRateLimiter(opts =>
    {
        opts.AddFixedWindowLimiter("auth", limiter =>
        {
            limiter.PermitLimit = builder.Configuration.GetValue("RateLimit:AuthEndpointsPermitLimit", 10);
            limiter.Window = TimeSpan.FromSeconds(builder.Configuration.GetValue("RateLimit:AuthEndpointsWindowSeconds", 60));
            limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiter.QueueLimit = 0;
        });
        opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });

    // --- JWT Authentication ---
    var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not configured.");
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew = TimeSpan.Zero
            };

            // Allow JWT from SignalR query string
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = ctx =>
                {
                    var token = ctx.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(token) && ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                        ctx.Token = token;
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();

    // --- Controllers ---
    builder.Services.AddControllers()
        .AddJsonOptions(opts =>
        {
            opts.JsonSerializerOptions.DefaultIgnoreCondition =
                System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });

    // --- SignalR ---
    builder.Services.AddSignalR();

    // --- Health Checks ---
    builder.Services.AddHealthChecks()
        .AddCheck<DatabaseHealthCheck>("database",
            failureStatus: HealthStatus.Unhealthy,
            tags: ["db", "ready"])
        .AddCheck<RedisHealthCheck>("cache",
            failureStatus: HealthStatus.Degraded,
            tags: ["cache", "ready"]);

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title       = "FoodFleet API",
            Version     = "v1",
            Description = "Multi-branch restaurant delivery platform API. " +
                          "Customer endpoints require a **Customer JWT**. " +
                          "Admin endpoints require an **Admin JWT** (Admin or SuperAdmin role).",
            Contact = new OpenApiContact { Name = "FoodFleet Team" }
        });

        // ── JWT Bearer security definition ────────────────────────────────
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name        = "Authorization",
            Type        = SecuritySchemeType.Http,
            Scheme      = "bearer",
            BearerFormat = "JWT",
            In          = ParameterLocation.Header,
            Description = "Paste your JWT token obtained from **POST /api/auth/login** " +
                          "(customer) or **POST /api/auth/admin/login** (admin)."
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });

        // ── XML doc comments ─────────────────────────────────────────────
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);

        // ── Organise endpoints into logical tag groups ────────────────────
        c.TagActionsBy(api =>
        {
            if (api.RelativePath!.StartsWith("api/auth/admin"))  return new[] { "Admin — Auth" };
            if (api.RelativePath!.StartsWith("api/admin/branches")) return new[] { "Admin — Branches" };
            if (api.RelativePath!.StartsWith("api/admin/menu"))  return new[] { "Admin — Menu" };
            if (api.RelativePath!.StartsWith("api/admin/orders")) return new[] { "Admin — Orders" };
            if (api.RelativePath!.StartsWith("api/admin/delivery-partners")) return new[] { "Admin — Delivery Partners" };
            if (api.RelativePath!.StartsWith("api/admin/settings")) return new[] { "Admin — Settings" };
            if (api.RelativePath!.StartsWith("api/admin/analytics")) return new[] { "Admin — Analytics" };
            if (api.RelativePath!.StartsWith("api/auth"))        return new[] { "Customer — Auth" };
            if (api.RelativePath!.StartsWith("api/branches"))    return new[] { "Public — Branches" };
            if (api.RelativePath!.StartsWith("api/restaurant"))  return new[] { "Public — Restaurant" };
            if (api.RelativePath!.StartsWith("api/geo"))         return new[] { "Public — Geo" };
            if (api.RelativePath!.StartsWith("api/customers"))   return new[] { "Customer — Profile" };
            if (api.RelativePath!.StartsWith("api/orders"))      return new[] { "Customer — Orders" };
            if (api.RelativePath!.StartsWith("api/payments"))    return new[] { "Customer — Payments" };
            if (api.RelativePath!.StartsWith("api/uploads"))     return new[] { "Uploads" };
            return new[] { api.ActionDescriptor.RouteValues["controller"] ?? "Other" };
        });
        c.DocInclusionPredicate((_, _) => true);
        c.OrderActionsBy(d => d.RelativePath);
    });

    var app = builder.Build();

    // --- Middleware pipeline ---
    app.UseMiddleware<GlobalExceptionMiddleware>();

    // Swagger available in all environments (restrict via network/auth in production)
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FoodFleet API v1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "FoodFleet API";
        c.DefaultModelsExpandDepth(-1);          // collapse schemas by default
        c.DisplayRequestDuration();
    });

    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FoodFleetDbContext>();
        db.Database.Migrate();
    }

    app.UseHttpsRedirection();
    app.UseSerilogRequestLogging();
    app.UseCors("AllowFrontends");
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHub<OrderHub>(OrderHub.Endpoint);

    // --- Health check endpoints ---
    // Liveness: is the process alive?
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false,   // no checks — if the app responds it's alive
        ResponseWriter = WriteJsonAsync
    });
    // Readiness: is the app ready to serve traffic?
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = hc => hc.Tags.Contains("ready"),
        ResponseWriter = WriteJsonAsync
    });
    // Full report
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = WriteJsonAsync
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application startup failed");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// ── Helpers ─────────────────────────────────────────────────────────────────

static Task WriteJsonAsync(HttpContext ctx, HealthReport report)
{
    ctx.Response.ContentType = "application/json";
    var result = new
    {
        status  = report.Status.ToString(),
        entries = report.Entries.ToDictionary(
            e => e.Key,
            e => new
            {
                status      = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration    = e.Value.Duration.TotalMilliseconds,
                error       = e.Value.Exception?.Message
            })
    };
    return ctx.Response.WriteAsync(JsonSerializer.Serialize(result,
        new JsonSerializerOptions { WriteIndented = false }));
}

// Make Program discoverable for WebApplicationFactory in integration tests
public partial class Program { }


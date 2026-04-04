using FoodFleet.Domain.Interfaces.Repositories;
using FoodFleet.Domain.Interfaces.Services;
using FoodFleet.Infrastructure.Data;
using FoodFleet.Infrastructure.Repositories;
using FoodFleet.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodFleet.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // --- Database ---
        services.AddDbContext<FoodFleetDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly(typeof(FoodFleetDbContext).Assembly.FullName)));

        // --- Repositories ---
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IRestaurantRepository, RestaurantRepository>();
        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<IMenuCategoryRepository, MenuCategoryRepository>();
        services.AddScoped<IMenuItemRepository, MenuItemRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ICustomerAddressRepository, CustomerAddressRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IDeliveryPartnerRepository, DeliveryPartnerRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();
        services.AddScoped<INotificationLogRepository, NotificationLogRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // --- Domain services ---
        services.AddScoped<IGeoService, GeoService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAdminCredentialStore, AdminCredentialStore>();
        services.AddScoped<IFileStorageService, AzureBlobStorageService>();
        services.AddScoped<IMenuExcelService, MenuExcelService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // --- Notification ---
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<ISmsService, SmsService>();
        services.AddScoped<INotificationService, NotificationService>();

        // --- Cache ---
        var redisConn = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConn))
        {
            services.AddStackExchangeRedisCache(options => options.Configuration = redisConn);
        }
        else
        {
            services.AddDistributedMemoryCache(); // fallback for development
        }
        services.AddScoped<ICacheService, RedisCacheService>();

        // --- Payment gateways ---
        services.AddScoped<CodPaymentGateway>();
        services.AddScoped<StripePaymentGateway>();
        services.AddScoped<RazorpayPaymentGateway>();

        // Resolve gateway by PaymentMethod at runtime via factory
        services.AddScoped<PaymentGatewayFactory>();

        // --- HTTP clients ---
        services.AddHttpClient("razorpay");

        // --- HTTP context accessor (for CurrentUserService) ---
        services.AddHttpContextAccessor();

        return services;
    }
}

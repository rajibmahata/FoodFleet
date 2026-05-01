using FoodFleet.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodFleet.Infrastructure.Data;

public class FoodFleetDbContext(DbContextOptions<FoodFleetDbContext> options) : DbContext(options)
{
    public DbSet<Restaurant> Restaurants { get; set; }
    public DbSet<Branch> Branches { get; set; }
    public DbSet<MenuCategory> MenuCategories { get; set; }
    public DbSet<MenuItem> MenuItems { get; set; }
    public DbSet<MenuItemVariant> MenuItemVariants { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<CustomerAddress> CustomerAddresses { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<OrderDelivery> OrderDeliveries { get; set; }
    public DbSet<DeliveryPartner> DeliveryPartners { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<RestaurantSettings> RestaurantSettings { get; set; }
    public DbSet<NotificationLog> NotificationLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Restaurant
        modelBuilder.Entity<Restaurant>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.ContactEmail).IsRequired().HasMaxLength(200);
            e.HasIndex(x => x.ContactEmail).IsUnique();
        });

        // Branch
        modelBuilder.Entity<Branch>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.Address).IsRequired().HasMaxLength(500);
            e.Property(x => x.DeliveryRadiusKm).HasDefaultValue(3.0);
            e.HasOne(x => x.Restaurant).WithMany(r => r.Branches).HasForeignKey(x => x.RestaurantId);
        });

        // MenuCategory
        modelBuilder.Entity<MenuCategory>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.HasOne(x => x.Branch).WithMany(b => b.MenuCategories).HasForeignKey(x => x.BranchId);
        });

        // MenuItem
        modelBuilder.Entity<MenuItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Price).HasColumnType("decimal(10,2)");
            e.HasOne(x => x.Category).WithMany(c => c.Items).HasForeignKey(x => x.CategoryId);
        });

        // MenuItemVariant
        modelBuilder.Entity<MenuItemVariant>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Label).IsRequired().HasMaxLength(100);
            e.Property(x => x.AdditionalPrice).HasColumnType("decimal(10,2)");
            e.HasOne(x => x.MenuItem).WithMany(i => i.Variants).HasForeignKey(x => x.MenuItemId);
        });

        // Customer
        modelBuilder.Entity<Customer>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.Email).IsRequired().HasMaxLength(200);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Phone).IsRequired().HasMaxLength(20);
        });

        // CustomerAddress
        modelBuilder.Entity<CustomerAddress>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Label).IsRequired().HasMaxLength(50);
            e.Property(x => x.FullAddress).IsRequired().HasMaxLength(500);
            e.HasOne(x => x.Customer).WithMany(c => c.Addresses).HasForeignKey(x => x.CustomerId);
        });

        // Order
        modelBuilder.Entity<Order>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TotalAmount).HasColumnType("decimal(10,2)");
            e.Property(x => x.DeliveryAddress).IsRequired().HasMaxLength(500);
            e.Property(x => x.Status).HasConversion<string>();
            e.Property(x => x.PaymentMethod).HasConversion<string>();
            e.HasOne(x => x.Customer).WithMany(c => c.Orders).HasForeignKey(x => x.CustomerId);
            e.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId);
            e.HasOne(x => x.Payment).WithOne(p => p.Order).HasForeignKey<Payment>(p => p.OrderId);
            e.HasOne(x => x.Delivery).WithOne(d => d.Order).HasForeignKey<OrderDelivery>(d => d.OrderId);
        });

        // OrderItem
        modelBuilder.Entity<OrderItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.UnitPrice).HasColumnType("decimal(10,2)");
            e.HasOne(x => x.Order).WithMany(o => o.Items).HasForeignKey(x => x.OrderId);
            e.HasOne(x => x.MenuItem).WithMany().HasForeignKey(x => x.MenuItemId);
            e.HasOne(x => x.Variant).WithMany().HasForeignKey(x => x.VariantId).IsRequired(false);
        });

        // OrderDelivery
        modelBuilder.Entity<OrderDelivery>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.DeliveryPartner).WithMany().HasForeignKey(x => x.DeliveryPartnerId);
        });

        // DeliveryPartner
        modelBuilder.Entity<DeliveryPartner>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.Phone).IsRequired().HasMaxLength(20);
            e.HasOne(x => x.Branch).WithMany(b => b.DeliveryPartners).HasForeignKey(x => x.BranchId);
        });

        // Payment
        modelBuilder.Entity<Payment>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasColumnType("decimal(10,2)");
            e.Property(x => x.Status).HasConversion<string>();
            e.Property(x => x.Gateway).IsRequired().HasMaxLength(50);
        });

        // RestaurantSettings
        modelBuilder.Entity<RestaurantSettings>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Key).IsRequired().HasMaxLength(100);
            e.Property(x => x.Value).HasMaxLength(2000);
            e.HasIndex(x => new { x.RestaurantId, x.Key }).IsUnique();
            e.HasOne(x => x.Restaurant).WithMany(r => r.Settings).HasForeignKey(x => x.RestaurantId);
        });

        // NotificationLog
        modelBuilder.Entity<NotificationLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.EventType).IsRequired().HasMaxLength(100);
            e.Property(x => x.Channel).HasConversion<string>();
            e.Property(x => x.Status).HasConversion<string>();
            e.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId);
        });
    }
}

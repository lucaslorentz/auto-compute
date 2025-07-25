using Microsoft.EntityFrameworkCore;

namespace LLL.AutoCompute.EFCore.Tests;

public class Customer
{
    public virtual required string Id { get; set; }
    public virtual ICollection<Order> Orders { get; protected set; } = [];
    public virtual int OrderCount { get; set; }
    public virtual decimal TotalSpent { get; set; }
}

public class Order
{
    public virtual int Id { get; protected set; }
    public virtual Customer? Customer { get; set; }
    public virtual ICollection<OrderItem> Items { get; protected set; } = [];
    public virtual decimal? Total { get; protected set; }
    public virtual Order? CloneFrom { get; set; }
    public virtual ICollection<Order> Clones { get; protected set; } = [];
}

public class OrderItem
{
    public virtual int Id { get; protected set; }
    public virtual Product? Product { get; set; }
    public virtual Order? Order { get; protected set; }
    public virtual decimal? Quantity { get; set; }
    public virtual decimal? UnitPrice { get; protected set; }
    public virtual decimal? Total { get; protected set; }
}

public class Product
{
    public virtual required string Id { get; set; }
    public virtual decimal? UnitPrice { get; set; }
}

class CommerceDbContext(DbContextOptions options)
    : DbContext(options), ITestDbContext<CommerceDbContext>
{
    public DbSet<Customer> Customers { get; set; } = default!;
    public DbSet<Order> Orders { get; set; } = default!;
    public DbSet<OrderItem> OrderItems { get; set; } = default!;
    public DbSet<Product> Products { get; set; } = default!;

    public object? ConfigurationKey => null;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var customerBuilder = modelBuilder.Entity<Customer>();

        customerBuilder.ComputedProperty(
            e => e.OrderCount,
            e => e.Orders.Count,
            c => c.NumberIncremental());

        customerBuilder.ComputedProperty(
            e => e.TotalSpent,
            e => e.Orders.Sum(o => o.Total) ?? 0,
            c => c.NumberIncremental());

        var orderBuilder = modelBuilder.Entity<Order>();
        orderBuilder.HasMany(e => e.Items).WithOne(e => e.Order);
        orderBuilder.HasOne(e => e.CloneFrom).WithMany(e => e.Clones);

        orderBuilder.ComputedProperty(
            e => e.Total,
            e => e.Items.Sum(i => i.Total));

        orderBuilder.ComputedNavigation(
            e => e.Items,
            e => e.CloneFrom != null
                ? e.CloneFrom.Items.Select(i => new OrderItem
                {
                    Product = i.Product,
                    Quantity = i.Quantity
                }).ToArray()
                : e.AsComputedUntracked().Items,
            c => c.CurrentValue(),
            c => c.ReuseItemsByKey(e => new { e.Product }));

        var orderItemBuilder = modelBuilder.Entity<OrderItem>();

        orderItemBuilder.ComputedProperty(
            e => e.Total,
            e => e.UnitPrice * e.Quantity);

        orderItemBuilder.ComputedProperty(
            e => e.UnitPrice,
            e => e.Product != null
                ? e.Product.AsComputedUntracked().UnitPrice
                : null);
    }

    public const string CustomerAId = "A";
    public const string ProductAId = "A";
    public const string ProductBId = "B";

    public void SeedData()
    {
        var customerA = new Customer
        {
            Id = CustomerAId
        };

        var productA = new Product
        {
            Id = CustomerAId,
            UnitPrice = 10
        };

        var productB = new Product
        {
            Id = ProductBId,
            UnitPrice = 5
        };

        var order1 = new Order
        {
            Customer = customerA,
            Items = {
                new OrderItem {
                    Product = productA,
                    Quantity = 2
                }
            }
        };

        Add(customerA);
        Add(productA);
        Add(productB);
        Add(order1);
    }

    public static CommerceDbContext Create(DbContextOptions options)
    {
        return new CommerceDbContext(options);
    }
}

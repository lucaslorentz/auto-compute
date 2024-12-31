using Microsoft.EntityFrameworkCore;

namespace LLL.AutoCompute.EFCore.Tests;

public class Customer
{
    public virtual required string Id { get; set; }
    public virtual ICollection<Order> Orders { get; protected set; } = [];
    public virtual int? OrderCount { get; set; }
    public virtual decimal? TotalSpent { get; set; }
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

class CommerceDbContext(
    DbContextOptions options,
    Action<ModelBuilder>? customizeModel
) : DbContext(options), ITestDbContext<CommerceDbContext>
{
    public DbSet<Customer> Customers { get; set; } = default!;
    public DbSet<Order> Orders { get; set; } = default!;
    public DbSet<OrderItem> OrderItems { get; set; } = default!;
    public DbSet<Product> Products { get; set; } = default!;

    public Action<ModelBuilder>? CustomizeModel => customizeModel;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var customerBuilder = modelBuilder.Entity<Customer>();
        customerBuilder.ComputedProperty(e => e.OrderCount, e => e.Orders.Count);
        customerBuilder.ComputedProperty(e => e.TotalSpent, e => e.Orders.Sum(o => o.Total));

        var orderBuilder = modelBuilder.Entity<Order>();
        orderBuilder.HasMany(e => e.Items).WithOne(e => e.Order);
        orderBuilder.HasOne(e => e.CloneFrom).WithMany(e => e.Clones);

        orderBuilder.ComputedProperty(e => e.Total, e => e.Items.Sum(i => i.Total));
        orderBuilder.Navigation(e => e.Items)
            .AutoCompute(e => e.CloneFrom != null
                ? e.CloneFrom.Items.Select(i => new OrderItem
                {
                    Product = i.Product,
                    Quantity = i.Quantity
                }).ToArray()
                : e.AsComputedUntracked().Items,
            c => c.CurrentValue(),
            c => c.ReuseItemsByKey(e => new { e.Product }));

        var orderItemBuilder = modelBuilder.Entity<OrderItem>();
        orderItemBuilder.ComputedProperty(e => e.Total, e => e.UnitPrice * e.Quantity);
        orderItemBuilder.ComputedProperty(e => e.UnitPrice, e => e.Product != null ? e.Product.AsComputedUntracked().UnitPrice : null);

        customizeModel?.Invoke(modelBuilder);
    }

    public static void SeedData(CommerceDbContext dbContext)
    {
    }

    public static CommerceDbContext Create(
        DbContextOptions options,
        Action<ModelBuilder>? customizeModel)
    {
        return new CommerceDbContext(options, customizeModel);
    }
}

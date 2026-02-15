using LLL.AutoCompute;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LLL.AutoCompute.EFCore.Tests.Changes;

public class NavigationDeferralTests
{
    [Fact]
    public async Task ChangePropagationTarget_LoadedEntities_UpdatesLoadedOrderOnly()
    {
        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<LoadedOnlyDeferralDbContext>()
            .UseSqlite(connection)
            .UseAutoCompute()
            .Options;

        await using (var setupDbContext = new LoadedOnlyDeferralDbContext(options))
        {
            await setupDbContext.Database.EnsureCreatedAsync();

            var product = new SharedProduct { Active = false };
            var orders = Enumerable.Range(0, 2).Select(_ => new SharedOrder
            {
                Items = [new SharedOrderItem { Product = product }]
            }).ToArray();

            setupDbContext.SharedOrders.AddRange(orders);
            await setupDbContext.SaveChangesAsync();
        }

        await using var dbContext = new LoadedOnlyDeferralDbContext(options);
        var order1 = await dbContext.SharedOrders
            .Include(e => e.Items)
            .ThenInclude(i => i.Product)
            .OrderBy(e => e.Id)
            .FirstAsync();

        var productToChange = order1.Items.Single().Product!;
        productToChange.Active = true;
        await dbContext.SaveChangesAsync();

        var allOrders = await dbContext.SharedOrders
            .OrderBy(e => e.Id)
            .ToArrayAsync();

        Assert.Equal(1, allOrders[0].ActiveItemsCount);
        Assert.Equal(0, allOrders[1].ActiveItemsCount);
    }

    [Fact]
    public async Task ChangePropagationTarget_LoadedEntities_ThrowsForDeferredInverseNavigationsInSameComputed()
    {
        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<DeferredInverseLoopDbContext>()
            .UseSqlite(connection)
            .UseAutoCompute()
            .Options;

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await using var dbContext = new DeferredInverseLoopDbContext(options);
            await dbContext.Database.EnsureCreatedAsync();
        });

        Assert.Contains("LoadedEntities", ex.Message);
        Assert.Contains("LoopOrder.Items", ex.Message);
        Assert.Contains("LoopItem.Order", ex.Message);
    }

    [Fact]
    public async Task ChangePropagationTarget_LoadedEntities_ThrowsWhenAffectsLoadedEntitiesAfterNonResolvingContext_MultiHop()
    {
        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<DeferredMultiHopLoopDbContext>()
            .UseSqlite(connection)
            .UseAutoCompute()
            .Options;

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await using var dbContext = new DeferredMultiHopLoopDbContext(options);
            await dbContext.Database.EnsureCreatedAsync();
        });

        Assert.Contains("LoadedEntities", ex.Message);
        Assert.Contains("HopB.C", ex.Message);
    }

    [Fact]
    public async Task ChangePropagationTarget_LoadedEntities_ThrowsWhenAffectsLoadedEntitiesAfterNonResolvingContext()
    {
        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<DeferredNoConflictDbContext>()
            .UseSqlite(connection)
            .UseAutoCompute()
            .Options;

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await using var dbContext = new DeferredNoConflictDbContext(options);
            await dbContext.Database.EnsureCreatedAsync();
        });

        Assert.Contains("LoadedEntities", ex.Message);
        Assert.Contains("NoConflictA.Bs", ex.Message);
        Assert.Contains("NoConflictC.Bs", ex.Message);
    }

    [Fact]
    public async Task ChangePropagationTarget_LoadedEntities_DoesNotThrowWhenNavigationHasNoInverse()
    {
        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<DeferredWithoutInverseDbContext>()
            .UseSqlite(connection)
            .UseAutoCompute()
            .Options;

        await using var dbContext = new DeferredWithoutInverseDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
    }
}

public class SharedOrder
{
    public int Id { get; protected set; }
    public int ActiveItemsCount { get; set; }
    public virtual ICollection<SharedOrderItem> Items { get; set; } = [];
}

public class SharedOrderItem
{
    public int Id { get; protected set; }
    public virtual SharedOrder? Order { get; set; }
    public virtual SharedProduct? Product { get; set; }
}

public class SharedProduct
{
    public int Id { get; protected set; }
    public bool Active { get; set; }
    public virtual ICollection<SharedOrderItem> OrderItems { get; set; } = [];
}

public class LoadedOnlyDeferralDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<SharedOrder> SharedOrders { get; set; } = default!;
    public DbSet<SharedOrderItem> SharedOrderItems { get; set; } = default!;
    public DbSet<SharedProduct> SharedProducts { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var orderBuilder = modelBuilder.Entity<SharedOrder>();
        orderBuilder
            .HasMany(e => e.Items)
            .WithOne(e => e.Order);
        orderBuilder.Navigation(e => e.Items).SetChangePropagationTarget(ChangePropagationTarget.LoadedEntities);
        orderBuilder.ComputedProperty(
            e => e.ActiveItemsCount,
            e => e.Items.Count(i => i.Product != null && i.Product.Active),
            c => c.NumberIncremental());

        modelBuilder.Entity<SharedOrderItem>()
            .HasOne(e => e.Product)
            .WithMany(e => e.OrderItems);
    }
}

public class LoopOrder
{
    public int Id { get; protected set; }
    public int LoopMetric { get; set; }
    public virtual ICollection<LoopItem> Items { get; set; } = [];
}

public class LoopItem
{
    public int Id { get; protected set; }
    public virtual LoopOrder? Order { get; set; }
}

public class DeferredInverseLoopDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<LoopOrder> LoopOrders { get; set; } = default!;
    public DbSet<LoopItem> LoopItems { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var orderBuilder = modelBuilder.Entity<LoopOrder>();
        orderBuilder
            .HasMany(e => e.Items)
            .WithOne(e => e.Order);
        orderBuilder.Navigation(e => e.Items).SetChangePropagationTarget(ChangePropagationTarget.LoadedEntities);

        var itemBuilder = modelBuilder.Entity<LoopItem>();
        itemBuilder.Navigation(e => e.Order).SetChangePropagationTarget(ChangePropagationTarget.LoadedEntities);

        orderBuilder.ComputedProperty(
            e => e.LoopMetric,
            e => e.Items.Sum(i => i.Order == null ? 0 : i.Order.Items.Count),
            c => c.NumberIncremental());
    }
}

public class HopA
{
    public int Id { get; protected set; }
    public int Metric { get; set; }
    public virtual ICollection<HopB> Bs { get; set; } = [];
}

public class HopB
{
    public int Id { get; protected set; }
    public virtual HopA? A { get; set; }
    public virtual HopC? C { get; set; }
}

public class HopC
{
    public int Id { get; protected set; }
    public virtual ICollection<HopB> Bs { get; set; } = [];
}

public class DeferredMultiHopLoopDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<HopA> HopAs { get; set; } = default!;
    public DbSet<HopB> HopBs { get; set; } = default!;
    public DbSet<HopC> HopCs { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var aBuilder = modelBuilder.Entity<HopA>();
        aBuilder
            .HasMany(e => e.Bs)
            .WithOne(e => e.A);
        aBuilder.Navigation(e => e.Bs).SetChangePropagationTarget(ChangePropagationTarget.LoadedEntities);

        var cBuilder = modelBuilder.Entity<HopC>();
        cBuilder
            .HasMany(e => e.Bs)
            .WithOne(e => e.C);
        cBuilder.Navigation(e => e.Bs).SetChangePropagationTarget(ChangePropagationTarget.LoadedEntities);

        modelBuilder.Entity<HopB>()
            .HasOne(e => e.A)
            .WithMany(e => e.Bs);

        modelBuilder.Entity<HopB>()
            .HasOne(e => e.C)
            .WithMany(e => e.Bs);

        aBuilder.ComputedProperty(
            e => e.Metric,
            e => e.Bs.Sum(b => b.C == null ? 0 : b.C.Bs.Count),
            c => c.NumberIncremental());
    }
}

public class NoConflictA
{
    public int Id { get; protected set; }
    public int Metric { get; set; }
    public virtual ICollection<NoConflictB> Bs { get; set; } = [];
}

public class NoConflictB
{
    public int Id { get; protected set; }
    public virtual NoConflictA? A { get; set; }
    public virtual NoConflictC? C { get; set; }
}

public class NoConflictC
{
    public int Id { get; protected set; }
    public virtual ICollection<NoConflictB> Bs { get; set; } = [];
}

public class DeferredNoConflictDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<NoConflictA> As { get; set; } = default!;
    public DbSet<NoConflictB> Bs { get; set; } = default!;
    public DbSet<NoConflictC> Cs { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var aBuilder = modelBuilder.Entity<NoConflictA>();
        aBuilder
            .HasMany(e => e.Bs)
            .WithOne(e => e.A);
        aBuilder.Navigation(e => e.Bs).SetChangePropagationTarget(ChangePropagationTarget.LoadedEntities);

        modelBuilder.Entity<NoConflictB>()
            .HasOne(e => e.C)
            .WithMany(e => e.Bs);

        modelBuilder.Entity<NoConflictB>()
            .Navigation(e => e.C)
            .SetChangePropagationTarget(ChangePropagationTarget.LoadedEntities);

        aBuilder.ComputedProperty(
            e => e.Metric,
            e => e.Bs.Sum(b => b.C == null ? 0 : b.C.Bs.Count),
            c => c.NumberIncremental());
    }
}

public class WithoutInverseA
{
    public int Id { get; protected set; }
    public int Metric { get; set; }
    public virtual ICollection<WithoutInverseB> Bs { get; set; } = [];
}

public class WithoutInverseB
{
    public int Id { get; protected set; }
    public virtual WithoutInverseA? A { get; set; }
    public virtual WithoutInverseC? C { get; set; }
}

public class WithoutInverseC
{
    public int Id { get; protected set; }
}

public class DeferredWithoutInverseDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<WithoutInverseA> As { get; set; } = default!;
    public DbSet<WithoutInverseB> Bs { get; set; } = default!;
    public DbSet<WithoutInverseC> Cs { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var aBuilder = modelBuilder.Entity<WithoutInverseA>();
        aBuilder
            .HasMany(e => e.Bs)
            .WithOne(e => e.A);
        aBuilder.Navigation(e => e.Bs).SetChangePropagationTarget(ChangePropagationTarget.LoadedEntities);

        modelBuilder.Entity<WithoutInverseB>()
            .HasOne(e => e.C)
            .WithMany();

        aBuilder.ComputedProperty(
            e => e.Metric,
            e => e.Bs.Count(b => b.C != null),
            c => c.NumberIncremental());
    }
}

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace LLL.AutoCompute.EFCore.Tests;

public static class TestDbContext
{
    public static async Task<TDbContext> Create<TDbContext>(
        Func<DbContext, Task>? seedData = null,
        bool useLazyLoadingProxies = true
    ) where TDbContext : DbContext, ICreatableTestDbContext<TDbContext>
    {
        return await Create(TDbContext.Create, seedData, useLazyLoadingProxies);
    }

    public static async Task<TDbContext> Create<TDbContext>(
        Func<DbContextOptions, TDbContext> factory,
        Func<DbContext, Task>? seedData = null,
        bool useLazyLoadingProxies = true
    ) where TDbContext : DbContext, ITestDbContext
    {
        var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();

        var contextOptions = new DbContextOptionsBuilder<TDbContext>()
            .UseSqlite(connection)
            .UseLazyLoadingProxies(useLazyLoadingProxies)
            .UseAutoCompute()
            .ReplaceService<IModelCacheKeyFactory, CustomizedModelCacheKeyFactory>()
            .Options;

        using (var context = factory(contextOptions))
        {
            await context.Database.EnsureCreatedAsync();

            context.SeedData();
            await context.SaveChangesAsync();

            if (seedData is not null)
            {
                await seedData(context);
                await context.SaveChangesAsync();
            }
        }

        return factory(contextOptions);
    }

    class CustomizedModelCacheKeyFactory : IModelCacheKeyFactory
    {
        public object Create(DbContext context, bool designTime)
        {
            return context is ITestDbContext testContext
            ? (context.GetType(), testContext.ConfigurationKey, designTime)
            : (object)context.GetType();
        }
    }
}
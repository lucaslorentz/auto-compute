using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace LLL.ComputedExpression.EFCore.Tests;

public static class TestDbContext
{
    public static async Task<TDbContext> Create<TDbContext>(
        Action<ModelBuilder>? customizeModel = null
    ) where TDbContext : DbContext, ITestDbContext<TDbContext>
    {
        var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();

        var contextOptions = new DbContextOptionsBuilder<TDbContext>()
            .UseSqlite(connection)
            .UseLazyLoadingProxies()
            .UseComputeds()
            .ReplaceService<IModelCacheKeyFactory, CustomizedModelCacheKeyFactory>()
            .Options;

        using (var context = TDbContext.Create(contextOptions, customizeModel))
        {
            await context.Database.EnsureCreatedAsync();
            TDbContext.SeedData(context);
            await context.SaveChangesAsync();
        }

        return TDbContext.Create(contextOptions, customizeModel);
    }

    class CustomizedModelCacheKeyFactory : IModelCacheKeyFactory
    {
        public object Create(DbContext context, bool designTime)
        {
            return context is ITestDbContext testContext
            ? (testContext.CustomizeModel, designTime)
            : (object)context.GetType();
        }
    }
}
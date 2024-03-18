using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LLL.Computed.EFCore.Tests;

public static class TestDbContext
{
    public static async Task<TDbContext> Create<TDbContext>(
    ) where TDbContext : DbContext, ISeededDbContext<TDbContext>
    {
        var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();

        var contextOptions = new DbContextOptionsBuilder<TDbContext>()
            .UseSqlite(connection)
            .UseLazyLoadingProxies()
            .UseComputeds()
            .Options;

        using (var context = (TDbContext)Activator.CreateInstance(typeof(TDbContext), contextOptions)!)
        {
            await context.Database.EnsureCreatedAsync();
            TDbContext.SeedData(context);
            await context.SaveChangesAsync();
        }

        return (TDbContext)Activator.CreateInstance(typeof(TDbContext), contextOptions)!;
    }
}

using Microsoft.EntityFrameworkCore;

namespace LLL.Computed.EFCore.Tests;

public interface ITestDbContext
{
    public Action<ModelBuilder>? CustomizeModel { get; }
}

public interface ITestDbContext<TDbContext> : ITestDbContext
    where TDbContext : DbContext, ITestDbContext<TDbContext>
{
    abstract static void SeedData(TDbContext dbContext);

    abstract static TDbContext Create(
        DbContextOptions options,
        Action<ModelBuilder>? customizeModel);
}

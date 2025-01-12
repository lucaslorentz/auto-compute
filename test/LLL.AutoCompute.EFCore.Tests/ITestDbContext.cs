using Microsoft.EntityFrameworkCore;

namespace LLL.AutoCompute.EFCore.Tests;

public interface ITestDbContext
{
    public object? ConfigurationKey { get; }
    abstract void SeedData();
}

public interface ICreatableTestDbContext<TDbContext> : ITestDbContext
{
    abstract static TDbContext Create(DbContextOptions options);
}

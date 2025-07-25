using Microsoft.EntityFrameworkCore;

namespace LLL.AutoCompute.EFCore.Tests;

public interface ITestDbContext
{
    public object? ConfigurationKey => "key";
    public void SeedData() { }
}

public interface ITestDbContext<TDbContext> : ITestDbContext
{
    abstract static TDbContext Create(DbContextOptions options);
}

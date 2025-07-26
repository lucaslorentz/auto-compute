using Microsoft.EntityFrameworkCore;

namespace LLL.AutoCompute.EFCore.Tests;

public class TestDbContext(DbContextOptions options) : DbContext(options), ITestDbContext<TestDbContext>
{
    public object? ConfigurationKey => "key";

    public static TestDbContext Create(DbContextOptions options)
    {
        return new TestDbContext(options);
    }

    public void SeedData()
    {
    }
}
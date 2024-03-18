using Microsoft.EntityFrameworkCore;

namespace LLL.Computed.EFCore.Tests;

public interface ISeededDbContext<TDbContext>
    where TDbContext : DbContext, ISeededDbContext<TDbContext>
{
    abstract static void SeedData(TDbContext dbContext);
}

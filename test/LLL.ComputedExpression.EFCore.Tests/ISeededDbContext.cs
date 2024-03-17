using Microsoft.EntityFrameworkCore;

namespace L3.Computed.EFCore.Tests;

public interface ISeededDbContext<TDbContext>
    where TDbContext : DbContext, ISeededDbContext<TDbContext>
{
    abstract static void SeedData(TDbContext dbContext);
}

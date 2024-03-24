using Microsoft.EntityFrameworkCore;

namespace LLL.Computed.EFCore.Internal;

public class EFCoreComputedInput(DbContext dbContext)
    : IEFCoreComputedInput
{
    public DbContext DbContext => dbContext;
}

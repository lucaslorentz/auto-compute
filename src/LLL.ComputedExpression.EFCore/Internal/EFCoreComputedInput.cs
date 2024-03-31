using Microsoft.EntityFrameworkCore;

namespace LLL.ComputedExpression.EFCore.Internal;

public class EFCoreComputedInput(DbContext dbContext)
    : IEFCoreComputedInput
{
    public DbContext DbContext => dbContext;
}

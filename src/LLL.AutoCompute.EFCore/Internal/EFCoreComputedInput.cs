using Microsoft.EntityFrameworkCore;

namespace LLL.AutoCompute.EFCore.Internal;

public class EFCoreComputedInput(DbContext dbContext, EFCoreChangeset changesToProcess)
    : ComputedInput
{
    public DbContext DbContext => dbContext;
    public EFCoreChangeset ChangesToProcess => changesToProcess;
}

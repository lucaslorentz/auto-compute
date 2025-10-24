using Microsoft.EntityFrameworkCore;

namespace LLL.AutoCompute.EFCore.Internal;

public class EFCoreComputedInput(DbContext dbContext, EFCoreChangeset changesToProcess)
    : ComputedInput, IEFCoreComputedInput
{
    public DbContext DbContext => dbContext;
    public EFCoreChangeset ChangesToProcess => changesToProcess;
}

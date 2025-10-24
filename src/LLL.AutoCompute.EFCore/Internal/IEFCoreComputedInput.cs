using Microsoft.EntityFrameworkCore;

namespace LLL.AutoCompute.EFCore.Internal;

public interface IEFCoreComputedInput : IComputedInput
{
    public DbContext DbContext { get; }
    public EFCoreChangeset ChangesToProcess { get; }
}

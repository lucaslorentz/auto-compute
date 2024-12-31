using Microsoft.EntityFrameworkCore;

namespace LLL.AutoCompute.EFCore.Internal;

public interface IEFCoreComputedInput
{
    DbContext DbContext { get; }
    EFCoreChangeset ChangesToProcess { get; }
}

using Microsoft.EntityFrameworkCore;

namespace LLL.Computed.EFCore.Internal;

public interface IEFCoreComputedInput
{
    DbContext DbContext { get; }
}

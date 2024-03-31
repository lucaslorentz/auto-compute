using Microsoft.EntityFrameworkCore;

namespace LLL.ComputedExpression.EFCore.Internal;

public interface IEFCoreComputedInput
{
    DbContext DbContext { get; }
}

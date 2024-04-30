using LLL.ComputedExpression.Caching;
using LLL.ComputedExpression.ChangesProviders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.ComputedExpression.EFCore.Internal;

public interface IEFCoreComputedInput : IDeltaChangesInput
{
    DbContext DbContext { get; }
    ILookup<ITypeBase, EntityEntry> ModifiedEntityEntries { get; }
    IConcurrentCreationCache Cache { get; }
    void Reset();
}

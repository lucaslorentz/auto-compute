using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using LLL.ComputedExpression.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.ComputedExpression.EFCore.Internal;

public interface IEFCoreComputedInput
{
    DbContext DbContext { get; }
    ILookup<ITypeBase, EntityEntry> ModifiedEntityEntries { get; }
    IConcurrentCreationCache Cache { get; }
    void Reset();
}

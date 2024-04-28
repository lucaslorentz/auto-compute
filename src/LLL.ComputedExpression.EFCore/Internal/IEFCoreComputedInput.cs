using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.ComputedExpression.EFCore.Internal;

public interface IEFCoreComputedInput
{
    DbContext DbContext { get; }
    HashSet<(object Entry, ISkipNavigation SkipNavigation, object RelatedEntry)> LoadedJoinEntities { get; }
    IEnumerable<EntityEntry> EntityEntriesOfType(ITypeBase entityType);
}

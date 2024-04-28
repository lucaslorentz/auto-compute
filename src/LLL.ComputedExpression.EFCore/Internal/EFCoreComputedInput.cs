using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.ComputedExpression.EFCore.Internal;

public class EFCoreComputedInput : IEFCoreComputedInput
{
    private readonly DbContext _dbContext;

    public EFCoreComputedInput(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public DbContext DbContext => _dbContext;

    public HashSet<(object Entry, ISkipNavigation SkipNavigation, object RelatedEntry)> LoadedJoinEntities { get; } = [];

    public IEnumerable<EntityEntry> EntityEntriesOfType(ITypeBase entityType)
    {
        return _dbContext.ChangeTracker.Entries().Where(e => e.Metadata == entityType);
    }
}

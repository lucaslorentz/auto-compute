using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Internal;

public class EFCoreComputedInput : IEFCoreComputedInput
{
    private readonly DbContext _dbContext;
    private ILookup<ITypeBase, EntityEntry>? _entriesByType = null;

    public EFCoreComputedInput(DbContext dbContext)
    {
        _dbContext = dbContext;
        _dbContext.ChangeTracker.DetectedEntityChanges += (o, e) =>
        {
            _entriesByType = null;
        };
    }

    public DbContext DbContext => _dbContext;

    public HashSet<(object Entry, ISkipNavigation SkipNavigation, object RelatedEntry)> LoadedJoinEntities { get; } = [];

    public IEnumerable<EntityEntry> EntityEntriesOfType(ITypeBase entityType)
    {
        _entriesByType ??= _dbContext.ChangeTracker.Entries().ToLookup(e => (ITypeBase)e.Metadata);
        return _entriesByType[entityType];
    }
}

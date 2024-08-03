using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Internal;

public class EFCoreComputedInput(DbContext dbContext) : IEFCoreComputedInput
{
    private readonly DbContext _dbContext = dbContext;

    public DbContext DbContext => _dbContext;

    public HashSet<(object Entry, ISkipNavigation SkipNavigation, object RelatedEntry)> LoadedJoinEntities { get; } = [];

    [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Optimisation to not create unecessary EntityEntry")]
    public IEnumerable<EntityEntry> EntityEntriesOfType(ITypeBase entityType)
    {
        if (DbContext.ChangeTracker.AutoDetectChangesEnabled)
            DbContext.ChangeTracker.DetectChanges();

        var dependencies = _dbContext.GetDependencies();
        return dependencies.StateManager
            .Entries
            .Where(e => e.EntityType == entityType)
            .Select(e => new EntityEntry(e));
    }
}

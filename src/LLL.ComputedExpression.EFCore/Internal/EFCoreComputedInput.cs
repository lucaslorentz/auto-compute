using System.Diagnostics.CodeAnalysis;
using LLL.ComputedExpression.Caching;
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
        Reset();
    }

    [MemberNotNull(nameof(ModifiedEntityEntries), nameof(Cache))]
    public void Reset()
    {
        ModifiedEntityEntries = _dbContext.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added
                || e.State == EntityState.Modified
                || e.State == EntityState.Deleted)
            .ToLookup(e => e.Metadata as ITypeBase);
        Cache = new ConcurrentCreationDictionary();
    }

    public DbContext DbContext => _dbContext;

    public ILookup<ITypeBase, EntityEntry> ModifiedEntityEntries { get; private set; }

    public IConcurrentCreationCache Cache { get; private set; }
}

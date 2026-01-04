using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Internal;

public class EFCoreObservedEntityType(IEntityType entityType)
    : IObservedEntityType
{
    public string Name => entityType.Name;

    public ObservedEntityState GetEntityState(ComputedInput input, object entity)
    {
        return input.Get<DbContext>().Entry(entity).State switch
        {
            EntityState.Added => ObservedEntityState.Added,
            EntityState.Deleted => ObservedEntityState.Removed,
            EntityState.Detached => ObservedEntityState.Removed,
            _ => ObservedEntityState.None
        };
    }

    public async Task CollectChangesAsync(DbContext dbContext, EFCoreChangeset changes)
    {
        foreach (var entityEntry in dbContext.EntityEntriesOfType(entityType))
        {
            await CollectChangesAsync(entityEntry, changes);
        }
    }

    public async Task CollectChangesAsync(EntityEntry entityEntry, EFCoreChangeset changes)
    {
        switch (entityEntry.State)
        {
            case EntityState.Added:
                changes.RegisterEntityAdded(entityType, entityEntry.Entity);
                break;
            case EntityState.Deleted:
            case EntityState.Detached:
                changes.RegisterEntityRemoved(entityType, entityEntry.Entity);
                break;
        }
    }

    public bool IsInstanceOfType(object obj)
    {
        return entityType.ClrType.IsInstanceOfType(obj);
    }

    public async Task<ObservedEntityTypeChange?> GetChangesAsync(ComputedInput input)
    {
        return input.Get<EFCoreChangeset>().GetChanges(entityType);
    }
}

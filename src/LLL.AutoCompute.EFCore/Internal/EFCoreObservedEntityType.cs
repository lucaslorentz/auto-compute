using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Internal;

public class EFCoreObservedEntityType(IEntityType entityType)
    : IObservedEntityType<IEFCoreComputedInput>
{
    public string Name => entityType.Name;

    public ObservedEntityState GetEntityState(IEFCoreComputedInput input, object entity)
    {
        return input.DbContext.Entry(entity).State switch
        {
            EntityState.Added => ObservedEntityState.Added,
            EntityState.Deleted => ObservedEntityState.Removed,
            EntityState.Detached => ObservedEntityState.Removed,
            _ => ObservedEntityState.None
        };
    }

    public bool IsInstanceOfType(object obj) {
        return entityType.ClrType.IsInstanceOfType(obj);
    }
}
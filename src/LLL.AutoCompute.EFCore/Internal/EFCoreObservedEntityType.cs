using Microsoft.EntityFrameworkCore;
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

    public bool IsInstanceOfType(object obj)
    {
        return entityType.ClrType.IsInstanceOfType(obj);
    }
}

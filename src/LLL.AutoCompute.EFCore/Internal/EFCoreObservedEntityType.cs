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
            Microsoft.EntityFrameworkCore.EntityState.Added => ObservedEntityState.Added,
            Microsoft.EntityFrameworkCore.EntityState.Deleted => ObservedEntityState.Removed,
            Microsoft.EntityFrameworkCore.EntityState.Detached => ObservedEntityState.Removed,
            _ => ObservedEntityState.None
        };
    }
}
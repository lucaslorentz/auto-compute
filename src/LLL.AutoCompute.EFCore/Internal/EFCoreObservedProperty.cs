using LLL.AutoCompute.EFCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Internal;

public class EFCoreObservedProperty(
    IProperty property)
    : EFCoreObservedMember, IObservedProperty
{
    public override IProperty Member => property;
    public override string Name => Member.Name;
    public virtual IObservedEntityType EntityType => Member.DeclaringType.ContainingEntityType.GetOrCreateObservedEntityType();

    public override string ToDebugString()
    {
        return $"{Member.DeclaringType.ShortName()}.{Member.Name}";
    }

    protected override object? GetOriginalValue(ComputedInput input, object ent, Func<object> currentValueGetter)
    {
        var entityState = EntityType.GetEntityState(input, ent);

        if (entityState == ObservedEntityState.Added)
            throw new Exception($"Cannot access property '{Member.DeclaringType.ShortName()}.{Member.Name}' original value for an added entity");

        var change = input.Get<EFCoreChangeset>().GetChange(Member, ent);
        if (change is not null)
            return change.OriginalValue;

        return currentValueGetter();
    }

    protected override object? GetCurrentValue(ComputedInput input, object ent, Func<object> currentValueGetter)
    {
        var entityState = EntityType.GetEntityState(input, ent);

        if (entityState == ObservedEntityState.Removed)
            throw new Exception($"Cannot access property '{Member.DeclaringType.ShortName()}.{Member.Name}' current value for a deleted entity");

        return currentValueGetter();
    }

    public override async Task CollectChangesAsync(DbContext dbContext, EFCoreChangeset changes)
    {
        foreach (var entityEntry in dbContext.EntityEntriesOfType(Member.DeclaringType))
        {
            await CollectChangesAsync(entityEntry, changes);
        }
    }

    public override async Task CollectChangesAsync(EntityEntry entityEntry, EFCoreChangeset changes)
    {
        if (entityEntry.State == EntityState.Added
            || entityEntry.State == EntityState.Deleted
            || entityEntry.State == EntityState.Modified)
        {
            var propertyEntry = entityEntry.Property(Member);
            if (entityEntry.State == EntityState.Added
                || entityEntry.State == EntityState.Deleted
                || propertyEntry.IsModified)
            {
                var originalValue = entityEntry.State == EntityState.Added
                    ? null
                    : propertyEntry.OriginalValue;

                var currentValue = entityEntry.State == EntityState.Deleted
                    ? null
                    : propertyEntry.CurrentValue;

                changes.RegisterPropertyChange(Member, entityEntry.Entity, originalValue, currentValue);
            }
        }
    }

    public async Task<IReadOnlyList<ObservedPropertyChange>> GetChangesAsync(ComputedInput input)
    {
        return input.Get<EFCoreChangeset>().GetChanges(Member);
    }
}

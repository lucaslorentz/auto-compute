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
    public virtual Type EntityType => Member.DeclaringType.ClrType;

    public override string ToDebugString()
    {
        return $"{Member.DeclaringType.ShortName()}.{Member.Name}";
    }

    protected override object? GetOriginalValue(ComputedInput input, object ent)
    {
        var dbContext = input.Get<DbContext>();

        var entityEntry = dbContext.Entry(ent!);

        if (entityEntry.State == EntityState.Added)
            throw new Exception($"Cannot access property '{Member.DeclaringType.ShortName()}.{Member.Name}' original value for an added entity");

        return entityEntry.Property(Member).OriginalValue;
    }

    protected override object? GetCurrentValue(ComputedInput input, object ent)
    {
        var dbContext = input.Get<DbContext>();

        var entityEntry = dbContext.Entry(ent!);

        if (entityEntry.State == EntityState.Deleted)
            throw new Exception($"Cannot access property '{Member.DeclaringType.ShortName()}.{Member.Name}' current value for a deleted entity");

        return entityEntry.Property(Member).CurrentValue;
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
                changes.AddPropertyChange(Member, entityEntry.Entity);
            }
        }
    }

    public async Task<ObservedPropertyChanges> GetChangesAsync(ComputedInput input)
    {
        return input.Get<EFCoreChangeset>().GetOrCreatePropertyChanges(Member);
    }
}

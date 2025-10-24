using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Internal;

public class EFCoreObservedProperty(
    IProperty property)
    : EFCoreObservedMember, IObservedProperty<EFCoreComputedInput>
{
    public override IProperty Property => property;
    public override string Name => Property.Name;
    public virtual Type EntityType => Property.DeclaringType.ClrType;

    public override string ToDebugString()
    {
        return $"{Property.DeclaringType.ShortName()}.{Property.Name}";
    }

    public override Expression CreateOriginalValueExpression(
        IObservedMemberAccess memberAccess,
        Expression inputExpression)
    {
        return Expression.Convert(
            Expression.Call(
                Expression.Constant(this),
                GetType().GetMethod(nameof(GetOriginalValue), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)!,
                inputExpression,
                memberAccess.FromExpression
            ),
            Property.ClrType
        );
    }

    public override Expression CreateCurrentValueExpression(
        IObservedMemberAccess memberAccess,
        Expression inputExpression)
    {
        return Expression.Convert(
            Expression.Call(
                Expression.Constant(this),
                GetType().GetMethod(nameof(GetCurrentValue), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)!,
                inputExpression,
                memberAccess.FromExpression
            ),
            Property.ClrType
        );
    }

    protected virtual object? GetOriginalValue(EFCoreComputedInput input, object ent)
    {
        var dbContext = input.DbContext;

        var entityEntry = dbContext.Entry(ent!);

        if (entityEntry.State == EntityState.Added)
            throw new Exception($"Cannot access property '{Property.DeclaringType.ShortName()}.{Property.Name}' original value for an added entity");

        return entityEntry.Property(Property).OriginalValue;
    }

    protected virtual object? GetCurrentValue(EFCoreComputedInput input, object ent)
    {
        var dbContext = input.DbContext;

        var entityEntry = dbContext.Entry(ent!);

        if (entityEntry.State == EntityState.Deleted)
            throw new Exception($"Cannot access property '{Property.DeclaringType.ShortName()}.{Property.Name}' current value for a deleted entity");

        return entityEntry.Property(Property).CurrentValue;
    }

    public override async Task CollectChangesAsync(DbContext dbContext, EFCoreChangeset changes)
    {
        foreach (var entityEntry in dbContext.EntityEntriesOfType(Property.DeclaringType))
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
            var propertyEntry = entityEntry.Property(Property);
            if (entityEntry.State == EntityState.Added
                || entityEntry.State == EntityState.Deleted
                || propertyEntry.IsModified)
            {
                changes.AddPropertyChange(Property, entityEntry.Entity);
            }
        }
    }

    public async Task<ObservedPropertyChanges> GetChangesAsync(EFCoreComputedInput input)
    {
        return input.ChangesToProcess.GetOrCreatePropertyChanges(Property);
    }
}

using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Internal;

public abstract class EFCoreObservedProperty(
    IProperty property)
    : EFCoreObservedMember
{
    public override IProperty Property => property;
}

public class EFCoreObservedProperty<TEntity>(
    IProperty property
) : EFCoreObservedProperty(property),
    IObservedProperty<IEFCoreComputedInput, TEntity>
    where TEntity : class
{
    public virtual string Name => Property.Name;

    public virtual string ToDebugString()
    {
        return $"{Property.DeclaringType.ShortName()}.{Property.Name}";
    }

    public virtual Expression CreateOriginalValueExpression(
        IObservedMemberAccess<IObservedProperty> memberAccess,
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

    public virtual Expression CreateCurrentValueExpression(
        IObservedMemberAccess<IObservedProperty> memberAccess,
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

    public Expression CreateIncrementalOriginalValueExpression(
        IObservedMemberAccess<IObservedProperty> memberAccess,
        Expression inputExpression,
        Expression incrementalContextExpression)
    {
        return CreateOriginalValueExpression(memberAccess, inputExpression);
    }

    public Expression CreateIncrementalCurrentValueExpression(
        IObservedMemberAccess<IObservedProperty> memberAccess,
        Expression inputExpression,
        Expression incrementalContextExpression)
    {
        return CreateCurrentValueExpression(memberAccess, inputExpression);
    }

    protected virtual object? GetOriginalValue(IEFCoreComputedInput input, TEntity ent)
    {
        var dbContext = input.DbContext;

        var entityEntry = dbContext.Entry(ent!);

        if (entityEntry.State == EntityState.Added)
            throw new Exception($"Cannot access property '{Property.DeclaringType.ShortName()}.{Property.Name}' original value for an added entity");

        return entityEntry.Property(Property).OriginalValue;
    }

    protected virtual object? GetCurrentValue(IEFCoreComputedInput input, TEntity ent)
    {
        var dbContext = input.DbContext;

        var entityEntry = dbContext.Entry(ent!);

        if (entityEntry.State == EntityState.Deleted)
            throw new Exception($"Cannot access property '{Property.DeclaringType.ShortName()}.{Property.Name}' current value for a deleted entity");

        return entityEntry.Property(Property).CurrentValue;
    }

    public virtual async Task<IReadOnlyCollection<TEntity>> GetAffectedEntitiesAsync(IEFCoreComputedInput input, IncrementalContext incrementalContext)
    {
        var affectedEntities = new HashSet<TEntity>();
        foreach (var entityEntry in input.EntityEntriesOfType(Property.DeclaringType))
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
                    affectedEntities.Add((TEntity)entityEntry.Entity);
                }
            }
        }
        return affectedEntities;
    }
}

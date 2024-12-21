using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Internal;

public class EFCoreObservedProperty(
    IProperty property)
    : EFCoreObservedMember, IObservedProperty<IEFCoreComputedInput>
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

    public override Expression CreateIncrementalOriginalValueExpression(
        IObservedMemberAccess memberAccess,
        Expression inputExpression,
        Expression incrementalContextExpression)
    {
        return CreateOriginalValueExpression(memberAccess, inputExpression);
    }

    public override Expression CreateIncrementalCurrentValueExpression(
        IObservedMemberAccess memberAccess,
        Expression inputExpression,
        Expression incrementalContextExpression)
    {
        return CreateCurrentValueExpression(memberAccess, inputExpression);
    }

    protected virtual object? GetOriginalValue(IEFCoreComputedInput input, object ent)
    {
        var dbContext = input.DbContext;

        var entityEntry = dbContext.Entry(ent!);

        if (entityEntry.State == Microsoft.EntityFrameworkCore.EntityState.Added)
            throw new Exception($"Cannot access property '{Property.DeclaringType.ShortName()}.{Property.Name}' original value for an added entity");

        return entityEntry.Property(Property).OriginalValue;
    }

    protected virtual object? GetCurrentValue(IEFCoreComputedInput input, object ent)
    {
        var dbContext = input.DbContext;

        var entityEntry = dbContext.Entry(ent!);

        if (entityEntry.State == Microsoft.EntityFrameworkCore.EntityState.Deleted)
            throw new Exception($"Cannot access property '{Property.DeclaringType.ShortName()}.{Property.Name}' current value for a deleted entity");

        return entityEntry.Property(Property).CurrentValue;
    }

    public override async Task<IReadOnlyCollection<object>> GetAffectedEntitiesAsync(IEFCoreComputedInput input, IncrementalContext incrementalContext)
    {
        var affectedEntities = new HashSet<object>();
        foreach (var entityEntry in input.EntityEntriesOfType(Property.DeclaringType))
        {
            if (entityEntry.State == Microsoft.EntityFrameworkCore.EntityState.Added
                || entityEntry.State == Microsoft.EntityFrameworkCore.EntityState.Deleted
                || entityEntry.State == Microsoft.EntityFrameworkCore.EntityState.Modified)
            {
                var propertyEntry = entityEntry.Property(Property);
                if (entityEntry.State == Microsoft.EntityFrameworkCore.EntityState.Added
                    || entityEntry.State == Microsoft.EntityFrameworkCore.EntityState.Deleted
                    || propertyEntry.IsModified)
                {
                    affectedEntities.Add(entityEntry.Entity);
                }
            }
        }
        return affectedEntities;
    }
}

using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Internal;

public class EFCoreEntityProperty<TEntity>(
    IProperty property
) : IEntityProperty<IEFCoreComputedInput, TEntity>
    where TEntity : class
{
    public virtual string Name => property.Name;

    public virtual string ToDebugString()
    {
        return $"{property.DeclaringType.ShortName()}.{property.Name}";
    }

    public virtual Expression CreateOriginalValueExpression(
        IEntityMemberAccess<IEntityProperty> memberAccess,
        Expression inputExpression)
    {
        return Expression.Convert(
            Expression.Call(
                Expression.Constant(this),
                GetType().GetMethod(nameof(GetOriginalValue), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)!,
                inputExpression,
                memberAccess.FromExpression
            ),
            property.ClrType
        );
    }

    public virtual Expression CreateCurrentValueExpression(
        IEntityMemberAccess<IEntityProperty> memberAccess,
        Expression inputExpression)
    {
        return Expression.Convert(
            Expression.Call(
                Expression.Constant(this),
                GetType().GetMethod(nameof(GetCurrentValue), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)!,
                inputExpression,
                memberAccess.FromExpression
            ),
            property.ClrType
        );
    }

    public Expression CreateIncrementalOriginalValueExpression(
        IEntityMemberAccess<IEntityProperty> memberAccess,
        Expression inputExpression,
        Expression incrementalContextExpression)
    {
        return CreateOriginalValueExpression(memberAccess, inputExpression);
    }

    public Expression CreateIncrementalCurrentValueExpression(
        IEntityMemberAccess<IEntityProperty> memberAccess,
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
            throw new Exception($"Cannot access property '{property.DeclaringEntityType.ShortName()}.{property.Name}' original value for an added entity");

        return entityEntry.Property(property).OriginalValue;
    }

    protected virtual object? GetCurrentValue(IEFCoreComputedInput input, TEntity ent)
    {
        var dbContext = input.DbContext;

        var entityEntry = dbContext.Entry(ent!);

        if (entityEntry.State == EntityState.Deleted)
            throw new Exception($"Cannot access property '{property.DeclaringEntityType.ShortName()}.{property.Name}' current value for a deleted entity");

        return entityEntry.Property(property).CurrentValue;
    }

    public virtual async Task<IReadOnlyCollection<TEntity>> GetAffectedEntitiesAsync(IEFCoreComputedInput input, IncrementalContext incrementalContext)
    {
        var affectedEntities = new HashSet<TEntity>();
        foreach (var entityEntry in input.EntityEntriesOfType(property.DeclaringType))
        {
            if (entityEntry.State == EntityState.Added
                || entityEntry.State == EntityState.Deleted
                || entityEntry.State == EntityState.Modified)
            {
                var propertyEntry = entityEntry.Property(property);
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

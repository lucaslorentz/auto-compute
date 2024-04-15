using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.ComputedExpression.EFCore.Internal;

public class EFCoreEntityProperty<TEntity>(
    IProperty property
) : IEntityProperty<IEFCoreComputedInput, TEntity>
{
    public virtual string Name => property.Name;

    public virtual string ToDebugString()
    {
        return $"{property.Name}";
    }

    public virtual IAffectedEntitiesProvider? GetAffectedEntitiesProvider()
    {
        return new EFCorePropertyAffectedEntitiesProvider<TEntity>(property);
    }

    public virtual Expression CreateOriginalValueExpression(
        IEntityMemberAccess<IEntityProperty> memberAccess,
        Expression inputExpression)
    {
        var valueGetter = static (IProperty property, IEFCoreComputedInput input, TEntity ent) =>
        {
            var dbContext = input.DbContext;

            var entityEntry = dbContext.Entry(ent!);

            if (entityEntry.State == EntityState.Added)
                throw new Exception($"Cannot access property '{property.DeclaringEntityType.ShortName()}.{property.Name}' original value for an added entity");

            return entityEntry.Property(property).OriginalValue;
        };

        return Expression.Convert(
            Expression.Invoke(
                Expression.Constant(valueGetter),
                Expression.Constant(property),
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
        var currentValueGetter = static (IProperty property, IEFCoreComputedInput input, TEntity ent) =>
        {
            var dbContext = input.DbContext;

            var entityEntry = dbContext.Entry(ent!);

            if (entityEntry.State == EntityState.Deleted)
                throw new Exception($"Cannot access property '{property.DeclaringEntityType.ShortName()}.{property.Name}' current value for a deleted entity");

            return entityEntry.Property(property).CurrentValue;
        };

        return Expression.Convert(
            Expression.Invoke(
                Expression.Constant(currentValueGetter),
                Expression.Constant(property),
                inputExpression,
                memberAccess.FromExpression
            ),
            property.ClrType
        );
    }
}

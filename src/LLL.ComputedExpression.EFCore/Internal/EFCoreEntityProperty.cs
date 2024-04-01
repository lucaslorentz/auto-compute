using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.ComputedExpression.EFCore.Internal;

public class EFCoreEntityProperty(IProperty property) : IEntityProperty
{
    public virtual string Name => property.Name;

    public virtual string ToDebugString()
    {
        return $"{property.Name}";
    }

    public virtual IAffectedEntitiesProvider? GetAffectedEntitiesProvider()
    {
        return new EFCorePropertyAffectedEntitiesProvider(property);
    }

    public virtual Expression CreatePreviousValueExpression(
        IEntityMemberAccess<IEntityProperty> memberAccess,
        Expression inputExpression)
    {
        var entityEntryExpression =
            Expression.Call(
                Expression.Property(
                    Expression.Convert(
                        inputExpression,
                        typeof(IEFCoreComputedInput)
                    ),
                    nameof(IEFCoreComputedInput.DbContext)
                ),
                nameof(DbContext.Entry),
                null,
                memberAccess.FromExpression
            );

        return Expression.Convert(
            Expression.Condition(
                Expression.Equal(
                    Expression.Property(
                        entityEntryExpression,
                        nameof(EntityEntry.State)
                    ),
                    Expression.Constant(EntityState.Added)
                ),
                Expression.Throw(
                    Expression.New(
                        typeof(Exception).GetConstructor([typeof(string)])!,
                        Expression.Constant("Cannot retrieve previous value from an added entity")
                    ),
                    typeof(object)
                ),
                Expression.Property(
                    Expression.Call(
                        entityEntryExpression,
                        nameof(EntityEntry.Property),
                        null,
                        Expression.Constant(property)
                    ),
                    nameof(PropertyEntry.OriginalValue)
                )
            ),
            property.ClrType
        );
    }
}
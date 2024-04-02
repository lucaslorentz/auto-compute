using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.ComputedExpression.EFCore.Internal;

public class EFCoreEntityNavigation(
    INavigation navigation
) : IEntityNavigation<IEFCoreComputedInput>
{
    public virtual string Name => navigation.Name;
    public virtual bool IsCollection => navigation.IsCollection;

    public virtual IEntityNavigation<IEFCoreComputedInput> GetInverse()
    {
        var inverse = navigation.Inverse
            ?? throw new InvalidOperationException($"No inverse for navigation '{navigation.DeclaringType.ShortName()}.{navigation.Name}'");

        return new EFCoreEntityNavigation(inverse);
    }

    public virtual async Task<IReadOnlyCollection<object>> LoadOriginalAsync(IEFCoreComputedInput input, IReadOnlyCollection<object> targetEntities)
    {
        var sourceEntities = new HashSet<object>();
        foreach (var targetEntity in targetEntities)
        {
            var targetEntry = input.DbContext.Entry(targetEntity);
            if (targetEntry.State == EntityState.Added)
                throw new Exception("Cannot get old value of an added entity");

            var navigationEntry = targetEntry.Navigation(navigation);

            if (!navigationEntry.IsLoaded)
                await navigationEntry.LoadAsync();

            foreach (var originalEntity in navigationEntry.GetOriginalEntities())
                sourceEntities.Add(originalEntity);
        }
        return sourceEntities;
    }

    public virtual async Task<IReadOnlyCollection<object>> LoadCurrentAsync(IEFCoreComputedInput input, IReadOnlyCollection<object> targetEntities)
    {
        var sourceEntities = new HashSet<object>();
        foreach (var targetEntity in targetEntities)
        {
            var navigationEntry = input.DbContext.Entry(targetEntity).Navigation(navigation);

            if (!navigationEntry.IsLoaded)
                await navigationEntry.LoadAsync();

            foreach (var entity in navigationEntry.GetEntities())
                sourceEntities.Add(entity);
        }
        return sourceEntities;
    }

    public virtual string ToDebugString()
    {
        return $"{navigation.Name}";
    }

    public virtual IAffectedEntitiesProvider? GetAffectedEntitiesProvider()
    {
        return new EFCoreNavigationAffectedEntitiesProvider(navigation);
    }

    public virtual Expression CreateOriginalValueExpression(
        IEntityMemberAccess<IEntityNavigation> memberAccess,
        Expression inputExpression)
    {
        var originalValueGetter = static (INavigation navigation, IEFCoreComputedInput input, object ent) =>
        {
            var dbContext = input.DbContext;

            var entityEntry = dbContext.Entry(ent);

            if (entityEntry.State == EntityState.Added)
                throw new InvalidOperationException("Cannot retrieve the original value of an added entity");

            return entityEntry.Navigation(navigation).GetOriginalValue();
        };

        return Expression.Convert(
            Expression.Invoke(
                Expression.Constant(originalValueGetter),
                Expression.Constant(navigation),
                inputExpression,
                memberAccess.FromExpression
            ),
            navigation.ClrType
        );
    }
}
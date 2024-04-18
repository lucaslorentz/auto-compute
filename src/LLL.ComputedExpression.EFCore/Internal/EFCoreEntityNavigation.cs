using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.ComputedExpression.EFCore.Internal;

public class EFCoreEntityNavigation<TSourceEntity, TTargetEntity>(
    INavigation navigation
) : IEntityNavigation<IEFCoreComputedInput, TSourceEntity, TTargetEntity>
    where TSourceEntity : class
    where TTargetEntity : class
{
    public virtual string Name => navigation.Name;
    public Type TargetEntityType => navigation.TargetEntityType.ClrType;
    public virtual bool IsCollection => navigation.IsCollection;

    public virtual IEntityNavigation<IEFCoreComputedInput, TTargetEntity, TSourceEntity> GetInverse()
    {
        var inverse = navigation.Inverse
            ?? throw new InvalidOperationException($"No inverse for navigation '{navigation.DeclaringType.ShortName()}.{navigation.Name}'");

        return new EFCoreEntityNavigation<TTargetEntity, TSourceEntity>(inverse);
    }

    public virtual async Task<IReadOnlyCollection<TTargetEntity>> LoadOriginalAsync(
        IEFCoreComputedInput input,
        IReadOnlyCollection<TSourceEntity> sourceEntities)
    {
        await input.DbContext.BulkLoadAsync(sourceEntities, navigation);

        var targetEntities = new HashSet<TTargetEntity>();
        foreach (var sourceEntity in sourceEntities)
        {
            var entityEntry = input.DbContext.Entry(sourceEntity!);
            if (entityEntry.State == EntityState.Added)
                throw new Exception($"Cannot access navigation '{navigation.DeclaringType.ShortName()}.{navigation.Name}' original value for an added entity");

            var navigationEntry = entityEntry.Navigation(navigation);

            foreach (var originalEntity in navigationEntry.GetOriginalEntities())
                targetEntities.Add((TTargetEntity)originalEntity);
        }
        return targetEntities;
    }

    public virtual async Task<IReadOnlyCollection<TTargetEntity>> LoadCurrentAsync(
        IEFCoreComputedInput input,
        IReadOnlyCollection<TSourceEntity> sourceEntities)
    {
        await input.DbContext.BulkLoadAsync(sourceEntities, navigation);

        var targetEntities = new HashSet<TTargetEntity>();
        foreach (var sourceEntitiy in sourceEntities)
        {
            var entityEntry = input.DbContext.Entry(sourceEntitiy!);
            var navigationEntry = entityEntry.Navigation(navigation);

            foreach (var entity in navigationEntry.GetEntities())
                targetEntities.Add((TTargetEntity)entity);
        }
        return targetEntities;
    }

    public virtual string ToDebugString()
    {
        return $"{navigation.Name}";
    }

    public virtual IAffectedEntitiesProvider? GetAffectedEntitiesProvider()
    {
        return new EFCoreNavigationAffectedEntitiesProvider<TSourceEntity>(navigation);
    }

    public virtual Expression CreateOriginalValueExpression(
        IEntityMemberAccess<IEntityNavigation> memberAccess,
        Expression inputExpression)
    {
        var valueGetter = static (INavigation navigation, IEFCoreComputedInput input, TSourceEntity ent) =>
        {
            var dbContext = input.DbContext;

            var entityEntry = dbContext.Entry(ent!);

            if (entityEntry.State == EntityState.Added)
                throw new Exception($"Cannot access navigation '{navigation.DeclaringType.ShortName()}.{navigation.Name}' original value for an added entity");

            var navigationEntry = entityEntry.Navigation(navigation);

            return navigationEntry.GetOriginalValue();
        };

        return Expression.Convert(
            Expression.Invoke(
                Expression.Constant(valueGetter),
                Expression.Constant(navigation),
                inputExpression,
                memberAccess.FromExpression
            ),
            navigation.ClrType
        );
    }

    public virtual Expression CreateCurrentValueExpression(
        IEntityMemberAccess<IEntityNavigation> memberAccess,
        Expression inputExpression)
    {
        var valueGetter = static (INavigation navigation, IEFCoreComputedInput input, TSourceEntity ent) =>
        {
            var dbContext = input.DbContext;

            var entityEntry = dbContext.Entry(ent!);

            if (entityEntry.State == EntityState.Deleted)
                throw new Exception($"Cannot access navigation '{navigation.DeclaringType.ShortName()}.{navigation.Name}' current value for a deleted entity");

            var navigationEntry = entityEntry.Navigation(navigation);
            if (!navigationEntry.IsLoaded && entityEntry.State != EntityState.Detached)
                navigationEntry.Load();

            return navigationEntry.CurrentValue;
        };

        return Expression.Convert(
            Expression.Invoke(
                Expression.Constant(valueGetter),
                Expression.Constant(navigation),
                inputExpression,
                memberAccess.FromExpression
            ),
            navigation.ClrType
        );
    }
}

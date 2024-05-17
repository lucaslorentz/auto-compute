using System.Collections;
using System.Linq.Expressions;
using LLL.ComputedExpression.EntityContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.ComputedExpression.EFCore.Internal;

public class EFCoreEntityNavigation<TSourceEntity, TTargetEntity>(
    INavigationBase navigation
) : IEntityNavigation<IEFCoreComputedInput, TSourceEntity, TTargetEntity>
    where TSourceEntity : class
    where TTargetEntity : class
{
    public virtual string Name => navigation.Name;
    public Type TargetEntityType => navigation.TargetEntityType.ClrType;
    public virtual bool IsCollection => navigation.IsCollection;

    public virtual string ToDebugString()
    {
        return $"{navigation.DeclaringEntityType.ShortName()}.{navigation.Name}";
    }

    public virtual IEntityNavigation<IEFCoreComputedInput, TTargetEntity, TSourceEntity> GetInverse()
    {
        var inverse = navigation.Inverse
            ?? throw new InvalidOperationException($"No inverse for navigation '{navigation.DeclaringType.ShortName()}.{navigation.Name}'");

        return new EFCoreEntityNavigation<TTargetEntity, TSourceEntity>(inverse);
    }

    public virtual async Task<IReadOnlyCollection<TTargetEntity>> LoadOriginalAsync(
        IEFCoreComputedInput input,
        IReadOnlyCollection<TSourceEntity> sourceEntities,
        IncrementalContext? incrementalContext)
    {
        await input.DbContext.BulkLoadAsync(sourceEntities, navigation);

        var targetEntities = new HashSet<TTargetEntity>();
        foreach (var sourceEntity in sourceEntities)
        {
            var entityEntry = input.DbContext.Entry(sourceEntity!);
            if (entityEntry.State == EntityState.Added)
                continue;

            var navigationEntry = entityEntry.Navigation(navigation);

            if (!navigationEntry.IsLoaded)
                await navigationEntry.LoadAsync();

            foreach (var originalEntity in navigationEntry.GetOriginalEntities())
            {
                targetEntities.Add((TTargetEntity)originalEntity);
                incrementalContext?.AddIncrementalEntity(sourceEntity, navigation, originalEntity);
                if (navigation.Inverse is not null)
                    incrementalContext?.AddIncrementalEntity(originalEntity, navigation.Inverse, sourceEntity);
            }
        }
        return targetEntities;
    }

    public virtual async Task<IReadOnlyCollection<TTargetEntity>> LoadCurrentAsync(
        IEFCoreComputedInput input,
        IReadOnlyCollection<TSourceEntity> sourceEntities,
        IncrementalContext? incrementalContext)
    {
        await input.DbContext.BulkLoadAsync(sourceEntities, navigation);

        var targetEntities = new HashSet<TTargetEntity>();
        foreach (var sourceEntity in sourceEntities)
        {
            var entityEntry = input.DbContext.Entry(sourceEntity!);
            var navigationEntry = entityEntry.Navigation(navigation);

            foreach (var entity in navigationEntry.GetEntities())
            {
                targetEntities.Add((TTargetEntity)entity);
                incrementalContext?.AddIncrementalEntity(sourceEntity, navigation, entity);
                if (navigation.Inverse is not null)
                    incrementalContext?.AddIncrementalEntity(entity, navigation.Inverse, sourceEntity);
            }
        }
        return targetEntities;
    }

    public virtual Expression CreateOriginalValueExpression(
        IEntityMemberAccess<IEntityNavigation> memberAccess,
        Expression inputExpression)
    {
        return Expression.Convert(
            Expression.Call(
                GetType().GetMethod(nameof(GetOriginalValue), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!,
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
        return Expression.Convert(
            Expression.Call(
                GetType().GetMethod(nameof(GetCurrentValue), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!,
                Expression.Constant(navigation),
                inputExpression,
                memberAccess.FromExpression
            ),
            navigation.ClrType
        );
    }

    public virtual Expression CreateIncrementalOriginalValueExpression(
        ComputedExpressionAnalysis analysis,
        IEntityMemberAccess<IEntityNavigation> memberAccess,
        Expression inputExpression,
        Expression incrementalContextExpression)
    {
        var entityContext = analysis.ResolveEntityContext(memberAccess.Expression,
            navigation.IsCollection ? EntityContextKeys.Element : EntityContextKeys.None);

        return Expression.Convert(
            Expression.Call(
                GetType().GetMethod(nameof(GetIncrementalOriginalValue), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!,
                Expression.Constant(navigation),
                inputExpression,
                memberAccess.FromExpression,
                Expression.Constant(entityContext, typeof(EntityContext)),
                incrementalContextExpression
            ),
            navigation.ClrType
        );
    }

    public virtual Expression CreateIncrementalCurrentValueExpression(
        ComputedExpressionAnalysis analysis,
        IEntityMemberAccess<IEntityNavigation> memberAccess,
        Expression inputExpression,
        Expression incrementalContextExpression)
    {
        var entityContext = analysis.ResolveEntityContext(memberAccess.Expression,
            navigation.IsCollection ? EntityContextKeys.Element : EntityContextKeys.None);

        return Expression.Convert(
            Expression.Call(
                GetType().GetMethod(nameof(GetIncrementalCurrentValue), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!,
                Expression.Constant(navigation),
                inputExpression,
                memberAccess.FromExpression,
                Expression.Constant(entityContext, typeof(EntityContext)),
                incrementalContextExpression
            ),
            navigation.ClrType
        );
    }

    private static object? GetOriginalValue(INavigationBase navigation, IEFCoreComputedInput input, TSourceEntity ent)
    {
        var dbContext = input.DbContext;

        var entityEntry = dbContext.Entry(ent!);

        if (entityEntry.State == EntityState.Added)
            throw new Exception($"Cannot access navigation '{navigation.DeclaringType.ShortName()}.{navigation.Name}' original value for an added entity");

        var navigationEntry = entityEntry.Navigation(navigation);

        if (!navigationEntry.IsLoaded)
            navigationEntry.Load();

        return navigationEntry.GetOriginalValue();
    }

    private static object? GetCurrentValue(INavigationBase navigation, IEFCoreComputedInput input, TSourceEntity ent)
    {
        var dbContext = input.DbContext;

        var entityEntry = dbContext.Entry(ent!);

        if (entityEntry.State == EntityState.Deleted)
            throw new Exception($"Cannot access navigation '{navigation.DeclaringType.ShortName()}.{navigation.Name}' current value for a deleted entity");

        var navigationEntry = entityEntry.Navigation(navigation);
        if (!navigationEntry.IsLoaded && entityEntry.State != EntityState.Detached)
            navigationEntry.Load();

        return navigationEntry.CurrentValue;
    }

    private static object? GetIncrementalOriginalValue(INavigationBase navigation, IEFCoreComputedInput input, TSourceEntity ent, EntityContext entityContext, IncrementalContext incrementalContext)
    {
        var entityEntry = input.DbContext.Entry(ent);

        if (entityEntry.State == EntityState.Added)
            throw new Exception($"Cannot access navigation '{navigation.DeclaringType.ShortName()}.{navigation.Name}' original value for an added entity");

        if (incrementalContext!.ShouldLoadAll(ent))
        {
            var value = GetOriginalValue(navigation, input, ent);
            if (value is IEnumerable enumerable)
            {
                foreach (var navEnt in enumerable)
                    incrementalContext.SetShouldLoadAll(navEnt);
            }
            else if (value is not null)
            {
                incrementalContext.SetShouldLoadAll(value);
            }
            return value;
        }

        var incrementalEntities = incrementalContext!.GetIncrementalEntities(ent, navigation);

        var principalEntry = input.DbContext.Entry(ent);

        var entities = incrementalEntities
            .Where(e =>
            {
                if (navigation is ISkipNavigation skipNavigation)
                {
                    var relatedEntry = input.DbContext.Entry(e);
                    skipNavigation.LoadJoinEntity(input, principalEntry, relatedEntry);
                    return skipNavigation.WasSkipRelated(
                        input,
                        principalEntry,
                        relatedEntry);
                }
                else if (navigation is INavigation directNav)
                {
                    return directNav.WasDirectlyRelated(
                        principalEntry,
                        input.DbContext.Entry(e));
                }

                throw new Exception("Navigation not supported");
            })
            .ToArray();

        if (navigation.IsCollection)
        {
            var collectionAccessor = navigation.GetCollectionAccessor()!;
            var collection = collectionAccessor.Create();
            foreach (var entity in entities)
                collectionAccessor.AddStandalone(collection, entity);
            return collection;
        }
        else
        {
            return entities.FirstOrDefault();
        }
    }

    private static object? GetIncrementalCurrentValue(INavigationBase navigation, IEFCoreComputedInput input, TSourceEntity ent, EntityContext entityContext, IncrementalContext incrementalContext)
    {
        var entityEntry = input.DbContext.Entry(ent);

        if (entityEntry.State == EntityState.Deleted)
            throw new Exception($"Cannot access navigation '{navigation.DeclaringType.ShortName()}.{navigation.Name}' current value for a deleted entity");

        if (incrementalContext!.ShouldLoadAll(ent))
        {
            var value = GetCurrentValue(navigation, input, ent);
            if (value is IEnumerable enumerable)
            {
                foreach (var navEnt in enumerable)
                    incrementalContext.SetShouldLoadAll(navEnt);
            }
            else if (value is not null)
            {
                incrementalContext.SetShouldLoadAll(value);
            }
            return value;
        }

        var incrementalEntities = incrementalContext!.GetIncrementalEntities(ent, navigation);

        var principalEntry = input.DbContext.Entry(ent);

        var entities = incrementalEntities
            .Where(e =>
            {
                if (navigation is ISkipNavigation skipNavigation)
                {
                    var relatedEntry = input.DbContext.Entry(e);
                    skipNavigation.LoadJoinEntity(input, principalEntry, relatedEntry);
                    return skipNavigation.IsSkipRelated(
                        input,
                        principalEntry,
                        relatedEntry);
                }
                else if (navigation is INavigation directNav)
                {
                    return directNav.IsDirectlyRelated(
                        principalEntry,
                        input.DbContext.Entry(e));
                }

                throw new Exception("Navigation not supported");
            })
            .ToArray();

        if (navigation.IsCollection)
        {
            var collectionAccessor = navigation.GetCollectionAccessor()!;
            var collection = collectionAccessor.Create();
            foreach (var entity in entities)
                collectionAccessor.AddStandalone(collection, entity);
            return collection;
        }
        else
        {
            return entities.FirstOrDefault();
        }
    }

    public IReadOnlyCollection<TTargetEntity> GetIncrementalEntities(IEFCoreComputedInput input, IReadOnlyCollection<TSourceEntity> fromEntities, IncrementalContext incrementalContext)
    {
        return fromEntities
            .SelectMany(e => incrementalContext.GetIncrementalEntities(e, navigation))
            .OfType<TTargetEntity>()
            .ToArray();
    }

    public async Task<IReadOnlyCollection<TSourceEntity>> GetAffectedEntitiesAsync(IEFCoreComputedInput input, IncrementalContext? incrementalContext)
    {
        var affectedEntities = new HashSet<TSourceEntity>();
        foreach (var entityEntry in input.EntityEntriesOfType(navigation.DeclaringEntityType))
        {
            if (entityEntry.State == EntityState.Added
                || entityEntry.State == EntityState.Deleted
                || entityEntry.State == EntityState.Modified)
            {
                var navigationEntry = entityEntry.Navigation(navigation);
                if (entityEntry.State == EntityState.Added
                    || entityEntry.State == EntityState.Deleted
                    || navigationEntry.IsModified)
                {
                    affectedEntities.Add((TSourceEntity)entityEntry.Entity);

                    if (entityEntry.State != EntityState.Added)
                    {
                        foreach (var ent in navigationEntry.GetOriginalEntities())
                        {
                            incrementalContext?.SetShouldLoadAll(ent);
                            incrementalContext?.AddIncrementalEntity(entityEntry.Entity, navigation, ent);
                        }
                    }

                    if (entityEntry.State != EntityState.Deleted)
                    {
                        foreach (var ent in navigationEntry.GetEntities())
                        {
                            incrementalContext?.SetShouldLoadAll(ent);
                            incrementalContext?.AddIncrementalEntity(entityEntry.Entity, navigation, ent);
                        }
                    }
                }
            }
        }
        if (navigation.Inverse is not null)
        {
            foreach (var entityEntry in input.EntityEntriesOfType(navigation.Inverse.DeclaringEntityType))
            {
                if (entityEntry.State == EntityState.Added
                    || entityEntry.State == EntityState.Deleted
                    || entityEntry.State == EntityState.Modified)
                {
                    var inverseNavigationEntry = entityEntry.Navigation(navigation.Inverse);
                    if (entityEntry.State == EntityState.Added
                        || entityEntry.State == EntityState.Deleted
                        || inverseNavigationEntry.IsModified)
                    {
                        if (!inverseNavigationEntry.IsLoaded && entityEntry.State != EntityState.Detached)
                            await inverseNavigationEntry.LoadAsync();

                        if (entityEntry.State != EntityState.Added)
                        {
                            foreach (var entity in inverseNavigationEntry.GetOriginalEntities())
                            {
                                affectedEntities.Add((TSourceEntity)entity);
                                incrementalContext?.SetShouldLoadAll(entityEntry.Entity);
                                incrementalContext?.AddIncrementalEntity(entity, navigation, entityEntry.Entity);
                            }
                        }

                        if (entityEntry.State != EntityState.Deleted)
                        {
                            foreach (var entity in inverseNavigationEntry.GetEntities())
                            {
                                affectedEntities.Add((TSourceEntity)entity);
                                incrementalContext?.SetShouldLoadAll(entityEntry.Entity);
                                incrementalContext?.AddIncrementalEntity(entity, navigation, entityEntry.Entity);
                            }
                        }
                    }
                }
            }
        }
        if (navigation is ISkipNavigation skipNavigation)
        {
            var dependentToPrincipal = skipNavigation.ForeignKey.DependentToPrincipal!;
            var joinReferenceToOther = skipNavigation.Inverse.ForeignKey.DependentToPrincipal;

            foreach (var joinEntry in input.EntityEntriesOfType(skipNavigation.JoinEntityType))
            {
                if (joinEntry.State == EntityState.Added
                    || joinEntry.State == EntityState.Deleted
                    || joinEntry.State == EntityState.Modified)
                {
                    var dependentToPrincipalEntry = joinEntry.Navigation(dependentToPrincipal);
                    var otherReferenceEntry = joinEntry.Reference(joinReferenceToOther!);

                    if (joinEntry.State == EntityState.Added
                        || joinEntry.State == EntityState.Deleted
                        || dependentToPrincipalEntry.IsModified)
                    {
                        if (!dependentToPrincipalEntry.IsLoaded && joinEntry.State != EntityState.Detached)
                            await dependentToPrincipalEntry.LoadAsync();

                        if (joinEntry.State != EntityState.Added)
                        {
                            foreach (var entity in dependentToPrincipalEntry.GetOriginalEntities())
                            {
                                affectedEntities.Add((TSourceEntity)entity);

                                foreach (var otherEntity in otherReferenceEntry.GetOriginalEntities())
                                {
                                    incrementalContext?.SetShouldLoadAll(otherEntity);
                                    incrementalContext?.AddIncrementalEntity(entity, navigation, otherEntity);
                                }
                            }
                        }

                        if (joinEntry.State != EntityState.Deleted)
                        {
                            foreach (var entity in dependentToPrincipalEntry.GetEntities())
                            {
                                affectedEntities.Add((TSourceEntity)entity);

                                foreach (var otherEntity in otherReferenceEntry.GetEntities())
                                {
                                    incrementalContext?.SetShouldLoadAll(otherEntity);
                                    incrementalContext?.AddIncrementalEntity(entity, navigation, otherEntity);
                                }
                            }
                        }
                    }
                }
            }
        }
        return affectedEntities;
    }
}

using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Internal;

public class EFCoreEntityNavigation<TSourceEntity, TTargetEntity>(
    INavigationBase navigation
) : IEntityNavigation<IEFCoreComputedInput, TSourceEntity, TTargetEntity>
    where TSourceEntity : class
    where TTargetEntity : class
{
    public virtual string Name => navigation.Name;
    public virtual Type TargetEntityType => navigation.TargetEntityType.ClrType;
    public virtual bool IsCollection => navigation.IsCollection;

    public virtual string ToDebugString()
    {
        return $"{navigation.DeclaringEntityType.ShortName()}.{navigation.Name}";
    }

    public virtual IEntityNavigation<IEFCoreComputedInput, TTargetEntity, TSourceEntity> GetInverse()
    {
        var inverse = navigation.Inverse
            ?? throw new InvalidOperationException($"No inverse for navigation '{navigation.DeclaringType.ShortName()}.{navigation.Name}'");

        return (EFCoreEntityNavigation<TTargetEntity, TSourceEntity>)inverse.GetEntityNavigation();
    }

    public virtual async Task<IReadOnlyCollection<TTargetEntity>> LoadOriginalAsync(
        IEFCoreComputedInput input,
        IReadOnlyCollection<TSourceEntity> sourceEntities,
        IncrementalContext incrementalContext)
    {
        await input.DbContext.BulkLoadAsync(sourceEntities, navigation);

        var targetEntities = new HashSet<TTargetEntity>();
        foreach (var sourceEntity in sourceEntities)
        {
            var entityEntry = input.DbContext.Entry(sourceEntity!);
            if (entityEntry.State == EntityState.Added)
                continue;

            var navigationEntry = entityEntry.Navigation(navigation);

            if (!navigationEntry.IsLoaded && entityEntry.State != EntityState.Detached)
                await navigationEntry.LoadAsync();

            foreach (var originalEntity in navigationEntry.GetOriginalEntities())
            {
                targetEntities.Add((TTargetEntity)originalEntity);
                incrementalContext?.AddIncrementalEntity(sourceEntity, this, originalEntity);
                if (navigation.Inverse is not null)
                    incrementalContext?.AddIncrementalEntity(originalEntity, GetInverse(), sourceEntity);
            }
        }
        return targetEntities;
    }

    public virtual async Task<IReadOnlyCollection<TTargetEntity>> LoadCurrentAsync(
        IEFCoreComputedInput input,
        IReadOnlyCollection<TSourceEntity> sourceEntities,
        IncrementalContext incrementalContext)
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
                incrementalContext?.AddIncrementalEntity(sourceEntity, this, entity);
                if (navigation.Inverse is not null)
                    incrementalContext?.AddIncrementalEntity(entity, GetInverse(), sourceEntity);
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
                Expression.Constant(this),
                GetType().GetMethod(nameof(GetOriginalValue), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)!,
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
                Expression.Constant(this),
                GetType().GetMethod(nameof(GetCurrentValue), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)!,
                inputExpression,
                memberAccess.FromExpression
            ),
            navigation.ClrType
        );
    }

    public virtual Expression CreateIncrementalOriginalValueExpression(
        IEntityMemberAccess<IEntityNavigation> memberAccess,
        Expression inputExpression,
        Expression incrementalContextExpression)
    {
        return Expression.Convert(
            Expression.Call(
                Expression.Constant(this),
                GetType().GetMethod(nameof(GetIncrementalOriginalValue), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)!,
                inputExpression,
                memberAccess.FromExpression,
                incrementalContextExpression
            ),
            navigation.ClrType
        );
    }

    public virtual Expression CreateIncrementalCurrentValueExpression(
        IEntityMemberAccess<IEntityNavigation> memberAccess,
        Expression inputExpression,
        Expression incrementalContextExpression)
    {
        return Expression.Convert(
            Expression.Call(
                Expression.Constant(this),
                GetType().GetMethod(nameof(GetIncrementalCurrentValue), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)!,
                inputExpression,
                memberAccess.FromExpression,
                incrementalContextExpression
            ),
            navigation.ClrType
        );
    }

    protected virtual object? GetOriginalValue(IEFCoreComputedInput input, TSourceEntity ent)
    {
        var dbContext = input.DbContext;

        var entityEntry = dbContext.Entry(ent!);

        if (entityEntry.State == EntityState.Added)
            throw new Exception($"Cannot access navigation '{navigation.DeclaringType.ShortName()}.{navigation.Name}' original value for an added entity");

        var navigationEntry = entityEntry.Navigation(navigation);

        if (!navigationEntry.IsLoaded && entityEntry.State != EntityState.Detached)
            navigationEntry.Load();

        return navigationEntry.GetOriginalValue();
    }

    protected virtual object? GetCurrentValue(IEFCoreComputedInput input, TSourceEntity ent)
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

    protected virtual object? GetIncrementalOriginalValue(IEFCoreComputedInput input, TSourceEntity ent, IncrementalContext incrementalContext)
    {
        var entityEntry = input.DbContext.Entry(ent);

        if (entityEntry.State == EntityState.Added)
            throw new Exception($"Cannot access navigation '{navigation.DeclaringType.ShortName()}.{navigation.Name}' original value for an added entity");

        var incrementalEntities = incrementalContext!.GetIncrementalEntities(ent, this);

        var principalEntry = input.DbContext.Entry(ent);

        var entities = incrementalEntities
            .Where(e =>
            {
                if (navigation is ISkipNavigation skipNavigation)
                {
                    var relatedEntry = input.DbContext.Entry(e);
                    skipNavigation.LoadJoinEntity(input, principalEntry, relatedEntry);
                    return skipNavigation.WasRelated(
                        input,
                        principalEntry,
                        relatedEntry);
                }
                else if (navigation is INavigation directNav)
                {
                    return directNav.WasRelated(
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

    protected virtual object? GetIncrementalCurrentValue(IEFCoreComputedInput input, TSourceEntity ent, IncrementalContext incrementalContext)
    {
        var entityEntry = input.DbContext.Entry(ent);

        if (entityEntry.State == EntityState.Deleted)
            throw new Exception($"Cannot access navigation '{navigation.DeclaringType.ShortName()}.{navigation.Name}' current value for a deleted entity");

        var incrementalEntities = incrementalContext!.GetIncrementalEntities(ent, this);

        var principalEntry = input.DbContext.Entry(ent);

        var entities = incrementalEntities
            .Where(e =>
            {
                if (navigation is ISkipNavigation skipNavigation)
                {
                    var relatedEntry = input.DbContext.Entry(e);
                    skipNavigation.LoadJoinEntity(input, principalEntry, relatedEntry);
                    return skipNavigation.IsRelated(
                        input,
                        principalEntry,
                        relatedEntry);
                }
                else if (navigation is INavigation directNav)
                {
                    return directNav.IsRelated(
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

    public virtual async Task<IReadOnlyCollection<TSourceEntity>> GetAffectedEntitiesAsync(IEFCoreComputedInput input, IncrementalContext incrementalContext)
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

                    var modifiedEntities = navigationEntry.GetModifiedEntities();

                    foreach (var ent in modifiedEntities)
                    {
                        incrementalContext?.SetShouldLoadAll(ent);
                        incrementalContext?.AddIncrementalEntity(entityEntry.Entity, this, ent);
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

                        var modifiedEntities = inverseNavigationEntry.GetModifiedEntities();

                        foreach (var entity in modifiedEntities)
                        {
                            affectedEntities.Add((TSourceEntity)entity);
                            incrementalContext?.SetShouldLoadAll(entityEntry.Entity);
                            incrementalContext?.AddIncrementalEntity(entity, this, entityEntry.Entity);
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
                                    incrementalContext?.AddIncrementalEntity(entity, this, otherEntity);
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
                                    incrementalContext?.AddIncrementalEntity(entity, this, otherEntity);
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

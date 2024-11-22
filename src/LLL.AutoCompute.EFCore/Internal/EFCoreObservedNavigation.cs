using System.Linq.Expressions;
using System.Reflection;
using LLL.AutoCompute.EFCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Internal;

public class EFCoreObservedNavigation(
    INavigationBase navigation)
    : EFCoreObservedMember, IObservedNavigation<IEFCoreComputedInput>
{
    public override INavigationBase Property => navigation;
    public INavigationBase Navigation => navigation;

    public virtual string Name => Navigation.Name;
    public virtual Type SourceEntityType => Navigation.DeclaringEntityType.ClrType;
    public virtual Type TargetEntityType => Navigation.TargetEntityType.ClrType;
    public virtual bool IsCollection => Navigation.IsCollection;

    public virtual string ToDebugString()
    {
        return $"{Navigation.DeclaringEntityType.ShortName()}.{Navigation.Name}";
    }

    public IObservedNavigation GetInverse()
    {
        var inverse = Navigation.Inverse
            ?? throw new InvalidOperationException($"No inverse for navigation '{Navigation.DeclaringType.ShortName()}.{Navigation.Name}'");

        return inverse.GetOrCreateObservedNavigation();
    }

    public virtual async Task<IReadOnlyCollection<object>> LoadOriginalAsync(
        IEFCoreComputedInput input,
        IReadOnlyCollection<object> sourceEntities,
        IncrementalContext incrementalContext)
    {
        await input.DbContext.BulkLoadAsync(sourceEntities, Navigation);

        var targetEntities = new HashSet<object>();
        foreach (var sourceEntity in sourceEntities)
        {
            var entityEntry = input.DbContext.Entry(sourceEntity!);
            if (entityEntry.State == EntityState.Added)
                continue;

            var navigationEntry = entityEntry.Navigation(Navigation);

            if (!navigationEntry.IsLoaded && entityEntry.State != EntityState.Detached)
                await navigationEntry.LoadAsync();

            foreach (var originalEntity in navigationEntry.GetOriginalEntities())
            {
                targetEntities.Add(originalEntity);
                incrementalContext?.AddIncrementalEntity(sourceEntity, this, originalEntity);
                if (Navigation.Inverse is not null)
                    incrementalContext?.AddIncrementalEntity(originalEntity, GetInverse(), sourceEntity);
            }
        }
        return targetEntities;
    }

    public async Task<IReadOnlyCollection<object>> LoadCurrentAsync(
        IEFCoreComputedInput input,
        IReadOnlyCollection<object> sourceEntities,
        IncrementalContext incrementalContext)
    {
        await input.DbContext.BulkLoadAsync(sourceEntities, Navigation);

        var targetEntities = new HashSet<object>();
        foreach (var sourceEntity in sourceEntities)
        {
            var entityEntry = input.DbContext.Entry(sourceEntity!);
            var navigationEntry = entityEntry.Navigation(Navigation);

            foreach (var entity in navigationEntry.GetEntities())
            {
                targetEntities.Add(entity);
                incrementalContext?.AddIncrementalEntity(sourceEntity, this, entity);
                if (Navigation.Inverse is not null)
                    incrementalContext?.AddIncrementalEntity(entity, GetInverse(), sourceEntity);
            }
        }
        return targetEntities;
    }

    public virtual Expression CreateOriginalValueExpression(
        IObservedMemberAccess<IObservedNavigation> memberAccess,
        Expression inputExpression)
    {
        return Expression.Convert(
            Expression.Call(
                Expression.Constant(this),
                GetType().GetMethod(nameof(GetOriginalValue), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)!,
                inputExpression,
                memberAccess.FromExpression
            ),
            Navigation.ClrType
        );
    }

    public virtual Expression CreateCurrentValueExpression(
        IObservedMemberAccess<IObservedNavigation> memberAccess,
        Expression inputExpression)
    {
        return Expression.Convert(
            Expression.Call(
                Expression.Constant(this),
                GetType().GetMethod(nameof(GetCurrentValue), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)!,
                inputExpression,
                memberAccess.FromExpression
            ),
            Navigation.ClrType
        );
    }

    public virtual Expression CreateIncrementalOriginalValueExpression(
        IObservedMemberAccess<IObservedNavigation> memberAccess,
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
            Navigation.ClrType
        );
    }

    public virtual Expression CreateIncrementalCurrentValueExpression(
        IObservedMemberAccess<IObservedNavigation> memberAccess,
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
            Navigation.ClrType
        );
    }

    protected virtual object? GetOriginalValue(IEFCoreComputedInput input, object ent)
    {
        var dbContext = input.DbContext;

        var entityEntry = dbContext.Entry(ent!);

        if (entityEntry.State == EntityState.Added)
            throw new Exception($"Cannot access navigation '{Navigation.DeclaringType.ShortName()}.{Navigation.Name}' original value for an added entity");

        var navigationEntry = entityEntry.Navigation(Navigation);

        if (!navigationEntry.IsLoaded && entityEntry.State != EntityState.Detached)
            navigationEntry.Load();

        return navigationEntry.GetOriginalValue();
    }

    protected virtual object? GetCurrentValue(IEFCoreComputedInput input, object ent)
    {
        var dbContext = input.DbContext;

        var entityEntry = dbContext.Entry(ent!);

        if (entityEntry.State == EntityState.Deleted)
            throw new Exception($"Cannot access navigation '{Navigation.DeclaringType.ShortName()}.{Navigation.Name}' current value for a deleted entity");

        var navigationEntry = entityEntry.Navigation(Navigation);
        if (!navigationEntry.IsLoaded && entityEntry.State != EntityState.Detached)
            navigationEntry.Load();

        return navigationEntry.CurrentValue;
    }

    protected virtual object? GetIncrementalOriginalValue(IEFCoreComputedInput input, object ent, IncrementalContext incrementalContext)
    {
        var entityEntry = input.DbContext.Entry(ent);

        if (entityEntry.State == EntityState.Added)
            throw new Exception($"Cannot access navigation '{Navigation.DeclaringType.ShortName()}.{Navigation.Name}' original value for an added entity");

        var incrementalEntities = incrementalContext!.GetIncrementalEntities(ent, this);

        var principalEntry = input.DbContext.Entry(ent);

        var entities = incrementalEntities
            .Where(e =>
            {
                if (Navigation is ISkipNavigation skipNavigation)
                {
                    var relatedEntry = input.DbContext.Entry(e);
                    skipNavigation.LoadJoinEntity(input, principalEntry, relatedEntry);
                    return skipNavigation.WasRelated(
                        input,
                        principalEntry,
                        relatedEntry);
                }
                else if (Navigation is INavigation directNav)
                {
                    return directNav.WasRelated(
                        principalEntry,
                        input.DbContext.Entry(e));
                }

                throw new Exception("Navigation not supported");
            })
            .ToArray();

        if (Navigation.IsCollection)
        {
            var collectionAccessor = Navigation.GetCollectionAccessor()!;
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

    protected virtual object? GetIncrementalCurrentValue(IEFCoreComputedInput input, object ent, IncrementalContext incrementalContext)
    {
        var entityEntry = input.DbContext.Entry(ent);

        if (entityEntry.State == EntityState.Deleted)
            throw new Exception($"Cannot access navigation '{Navigation.DeclaringType.ShortName()}.{Navigation.Name}' current value for a deleted entity");

        var incrementalEntities = incrementalContext!.GetIncrementalEntities(ent, this);

        var principalEntry = input.DbContext.Entry(ent);

        var entities = incrementalEntities
            .Where(e =>
            {
                if (Navigation is ISkipNavigation skipNavigation)
                {
                    var relatedEntry = input.DbContext.Entry(e);
                    skipNavigation.LoadJoinEntity(input, principalEntry, relatedEntry);
                    return skipNavigation.IsRelated(
                        input,
                        principalEntry,
                        relatedEntry);
                }
                else if (Navigation is INavigation directNav)
                {
                    return directNav.IsRelated(
                        principalEntry,
                        input.DbContext.Entry(e));
                }

                throw new Exception("Navigation not supported");
            })
            .ToArray();

        if (Navigation.IsCollection)
        {
            var collectionAccessor = Navigation.GetCollectionAccessor()!;
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

    public virtual async Task<IReadOnlyCollection<object>> GetAffectedEntitiesAsync(IEFCoreComputedInput input, IncrementalContext incrementalContext)
    {
        var affectedEntities = new HashSet<object>();
        foreach (var entityEntry in input.EntityEntriesOfType(Navigation.DeclaringEntityType))
        {
            if (entityEntry.State == EntityState.Added
                || entityEntry.State == EntityState.Deleted
                || entityEntry.State == EntityState.Modified)
            {
                var navigationEntry = entityEntry.Navigation(Navigation);
                if (entityEntry.State == EntityState.Added
                    || entityEntry.State == EntityState.Deleted
                    || navigationEntry.IsModified)
                {
                    affectedEntities.Add(entityEntry.Entity);

                    var modifiedEntities = navigationEntry.GetModifiedEntities();

                    foreach (var ent in modifiedEntities)
                    {
                        incrementalContext?.SetShouldLoadAll(ent);
                        incrementalContext?.AddIncrementalEntity(entityEntry.Entity, this, ent);
                    }
                }
            }
        }
        if (Navigation.Inverse is not null)
        {
            foreach (var entityEntry in input.EntityEntriesOfType(Navigation.Inverse.DeclaringEntityType))
            {
                if (entityEntry.State == EntityState.Added
                    || entityEntry.State == EntityState.Deleted
                    || entityEntry.State == EntityState.Modified)
                {
                    var inverseNavigationEntry = entityEntry.Navigation(Navigation.Inverse);
                    if (entityEntry.State == EntityState.Added
                        || entityEntry.State == EntityState.Deleted
                        || inverseNavigationEntry.IsModified)
                    {
                        if (!inverseNavigationEntry.IsLoaded && entityEntry.State != EntityState.Detached)
                            await inverseNavigationEntry.LoadAsync();

                        var modifiedEntities = inverseNavigationEntry.GetModifiedEntities();

                        foreach (var entity in modifiedEntities)
                        {
                            affectedEntities.Add(entity);
                            incrementalContext?.SetShouldLoadAll(entityEntry.Entity);
                            incrementalContext?.AddIncrementalEntity(entity, this, entityEntry.Entity);
                        }
                    }
                }
            }
        }
        if (Navigation is ISkipNavigation skipNavigation)
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
                                affectedEntities.Add(entity);

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
                                affectedEntities.Add(entity);

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

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

    public override string Name => Navigation.Name;
    public virtual IObservedEntityType SourceEntityType => Navigation.DeclaringEntityType.GetOrCreateObservedEntityType();
    public virtual IObservedEntityType TargetEntityType => Navigation.TargetEntityType.GetOrCreateObservedEntityType();
    public virtual bool IsCollection => Navigation.IsCollection;

    public override string ToDebugString()
    {
        return $"{Navigation.DeclaringEntityType.ShortName()}.{Navigation.Name}";
    }

    public IObservedNavigation GetInverse()
    {
        var inverse = Navigation.Inverse
            ?? throw new InvalidOperationException($"No inverse for navigation '{Navigation.DeclaringType.ShortName()}.{Navigation.Name}'");

        return inverse.GetOrCreateObservedNavigation();
    }

    public virtual async Task<IReadOnlyDictionary<object, IReadOnlyCollection<object>>> LoadOriginalAsync(
        IEFCoreComputedInput input,
        IReadOnlyCollection<object> sourceEntities)
    {
        await input.DbContext.BulkLoadAsync(sourceEntities, Navigation);

        var targetEntities = new Dictionary<object, IReadOnlyCollection<object>>();
        foreach (var sourceEntity in sourceEntities)
        {
            var entityEntry = input.DbContext.Entry(sourceEntity!);
            if (entityEntry.State == EntityState.Added)
                continue;

            var navigationEntry = entityEntry.Navigation(Navigation);

            if (!navigationEntry.IsLoaded && entityEntry.State != EntityState.Detached)
                await navigationEntry.LoadAsync();

            targetEntities.Add(sourceEntity, navigationEntry.GetOriginalEntities());
        }
        return targetEntities;
    }

    public async Task<IReadOnlyDictionary<object, IReadOnlyCollection<object>>> LoadCurrentAsync(
        IEFCoreComputedInput input,
        IReadOnlyCollection<object> sourceEntities)
    {
        await input.DbContext.BulkLoadAsync(sourceEntities, Navigation);

        var targetEntities = new Dictionary<object, IReadOnlyCollection<object>>();
        foreach (var sourceEntity in sourceEntities)
        {
            var entityEntry = input.DbContext.Entry(sourceEntity!);
            var navigationEntry = entityEntry.Navigation(Navigation);

            if (!navigationEntry.IsLoaded && entityEntry.State != EntityState.Detached)
                await navigationEntry.LoadAsync();

            targetEntities.Add(sourceEntity, navigationEntry.GetCurrentEntities());
        }
        return targetEntities;
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
            Navigation.ClrType
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
            Navigation.ClrType
        );
    }

    public override Expression CreateIncrementalOriginalValueExpression(
        IObservedMemberAccess memberAccess,
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

    public override Expression CreateIncrementalCurrentValueExpression(
        IObservedMemberAccess memberAccess,
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

        return navigationEntry.GetCurrentValue();
    }

    protected virtual object? GetIncrementalOriginalValue(IEFCoreComputedInput input, object ent, IncrementalContext incrementalContext)
    {
        var entityEntry = input.DbContext.Entry(ent);

        if (entityEntry.State == EntityState.Added)
            throw new Exception($"Cannot access navigation '{Navigation.DeclaringType.ShortName()}.{Navigation.Name}' original value for an added entity");

        var incrementalEntities = incrementalContext.GetOriginalEntities(ent, this);

        if (Navigation.IsCollection)
        {
            var collectionAccessor = Navigation.GetCollectionAccessor()!;
            var collection = collectionAccessor.Create();
            foreach (var entity in incrementalEntities)
                collectionAccessor.AddStandalone(collection, entity);
            return collection;
        }
        else
        {
            return incrementalEntities.FirstOrDefault();
        }
    }

    protected virtual object? GetIncrementalCurrentValue(IEFCoreComputedInput input, object ent, IncrementalContext incrementalContext)
    {
        var entityEntry = input.DbContext.Entry(ent);

        if (entityEntry.State == EntityState.Deleted)
            throw new Exception($"Cannot access navigation '{Navigation.DeclaringType.ShortName()}.{Navigation.Name}' current value for a deleted entity");

        var incrementalEntities = incrementalContext.GetCurrentEntities(ent, this);

        if (Navigation.IsCollection)
        {
            var collectionAccessor = Navigation.GetCollectionAccessor()!;
            var collection = collectionAccessor.Create();
            foreach (var entity in incrementalEntities)
                collectionAccessor.AddStandalone(collection, entity);
            return collection;
        }
        else
        {
            return incrementalEntities.FirstOrDefault();
        }
    }

    public async Task<ObservedNavigationChanges> GetChangesAsync(IEFCoreComputedInput input)
    {
        var changes = new ObservedNavigationChanges();
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
                    var modifiedEntities = navigationEntry.GetModifiedEntities();

                    foreach (var ent in modifiedEntities.added)
                        changes.RegisterAdded(entityEntry.Entity, ent);

                    foreach (var ent in modifiedEntities.removed)
                        changes.RegisterRemoved(entityEntry.Entity, ent);
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

                        foreach (var entity in modifiedEntities.added)
                            changes.RegisterAdded(entity, entityEntry.Entity);

                        foreach (var entity in modifiedEntities.removed)
                            changes.RegisterRemoved(entity, entityEntry.Entity);
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

                        if (joinEntry.State == EntityState.Added
                            || dependentToPrincipalEntry.IsModified)
                        {
                            foreach (var entity in dependentToPrincipalEntry.GetCurrentEntities())
                            {
                                foreach (var otherEntity in otherReferenceEntry.GetCurrentEntities())
                                    changes.RegisterAdded(entity, otherEntity);
                            }
                        }

                        if (joinEntry.State == EntityState.Deleted
                            || dependentToPrincipalEntry.IsModified)
                        {
                            foreach (var entity in dependentToPrincipalEntry.GetOriginalEntities())
                            {
                                foreach (var otherEntity in otherReferenceEntry.GetOriginalEntities())
                                    changes.RegisterRemoved(entity, otherEntity);
                            }
                        }
                    }
                }
            }
        }
        return changes;
    }
}

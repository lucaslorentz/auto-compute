using System.Collections;
using LLL.AutoCompute.EFCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Internal;

public class EFCoreObservedNavigation(
    INavigationBase navigation)
    : EFCoreObservedMember, IObservedNavigation
{
    public override INavigationBase Member => navigation;

    public override string Name => Member.Name;
    public virtual IObservedEntityType SourceEntityType => Member.DeclaringEntityType.GetOrCreateObservedEntityType();
    public virtual IObservedEntityType TargetEntityType => Member.TargetEntityType.GetOrCreateObservedEntityType();
    public virtual bool IsCollection => Member.IsCollection;
    public virtual ChangePropagationTarget? ChangePropagationTarget => Member.GetChangePropagationTarget();

    public override string ToDebugString()
    {
        return $"{Member.DeclaringEntityType.ShortName()}.{Member.Name}";
    }

    public IObservedNavigation? GetInverse()
    {
        var inverse = Member.Inverse;
        if (inverse is null || inverse.IsShadowProperty())
            return null;

        return inverse.GetOrCreateObservedNavigation();
    }

    public IObservedNavigation GetInverseOrThrow()
    {
        return GetInverse()
            ?? throw new InvalidOperationException(
                $"Navigation '{ToDebugString()}' requires a non-shadow inverse navigation.");
    }

    public virtual async Task<IReadOnlyDictionary<object, IReadOnlyCollection<object>>> LoadOriginalAsync(
        ComputedInput input,
        IReadOnlyCollection<object> sourceEntities)
    {
        var dbContext = input.Get<DbContext>();

        await dbContext.BulkLoadAsync(sourceEntities, Member);

        var targetEntities = new Dictionary<object, IReadOnlyCollection<object>>();
        foreach (var sourceEntity in sourceEntities)
        {
            var entityEntry = dbContext.Entry(sourceEntity!);
            if (entityEntry.State == EntityState.Added)
                continue;

            var navigationEntry = entityEntry.Navigation(Member);

            if (!navigationEntry.IsLoaded && entityEntry.State != EntityState.Detached)
                await navigationEntry.LoadAsync();

            targetEntities.Add(sourceEntity, navigationEntry.GetOriginalEntities());
        }
        return targetEntities;
    }

    public async Task<IReadOnlyDictionary<object, IReadOnlyCollection<object>>> LoadCurrentAsync(
        ComputedInput input,
        IReadOnlyCollection<object> sourceEntities)
    {
        var dbContext = input.Get<DbContext>();

        await dbContext.BulkLoadAsync(sourceEntities, Member);

        var targetEntities = new Dictionary<object, IReadOnlyCollection<object>>();
        foreach (var sourceEntity in sourceEntities)
        {
            var entityEntry = dbContext.Entry(sourceEntity!);
            var navigationEntry = entityEntry.Navigation(Member);

            if (!navigationEntry.IsLoaded && entityEntry.State != EntityState.Detached)
                await navigationEntry.LoadAsync();

            targetEntities.Add(sourceEntity, navigationEntry.GetCurrentEntities());
        }
        return targetEntities;
    }

    protected override object? GetOriginalValue(ComputedInput input, object ent, Func<object> currentValueGetter)
    {
        var dbContext = input.Get<DbContext>();

        var entityEntry = dbContext.Entry(ent!);

        if (entityEntry.State == EntityState.Added)
            throw new Exception($"Cannot access navigation '{Member.DeclaringType.ShortName()}.{Member.Name}' original value for an added entity");

        if (input.TryGet<IncrementalContext>(out var incrementalContext))
        {
            var incrementalEntities = incrementalContext.GetOriginalEntities(ent, this);

            if (Member.IsCollection)
            {
                var collectionAccessor = Member.GetCollectionAccessor()!;
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
        else
        {
            var navigationEntry = entityEntry.Navigation(Member);
            var original = navigationEntry.GetCurrentValue();
            var change = input.Get<EFCoreChangeset>().GetChange(Member, ent);
            // Undo the changes from changeset
            if (change is not null)
            {
                foreach (var a in change.Added)
                {
                    if (Member.IsCollection)
                        ((IList)original!).Remove(a);
                    else
                        original = null;
                }
                foreach (var r in change.Removed)
                {
                    if (Member.IsCollection)
                        ((IList)original!).Add(r);
                    else
                        original = r;
                }
            }
            return original;
        }
    }

    protected override object? GetCurrentValue(ComputedInput input, object ent, Func<object> currentValueGetter)
    {
        var dbContext = input.Get<DbContext>();

        var entityState = SourceEntityType.GetEntityState(input, ent);

        if (entityState == ObservedEntityState.Removed)
            throw new Exception($"Cannot access navigation '{Member.DeclaringType.ShortName()}.{Member.Name}' current value for a deleted entity");

        if (input.TryGet<IncrementalContext>(out var incrementalContext))
        {
            var incrementalEntities = incrementalContext.GetCurrentEntities(ent, this);

            if (Member.IsCollection)
            {
                var collectionAccessor = Member.GetCollectionAccessor()!;
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
        else
        {
            var entityEntry = dbContext.Entry(ent!);
            var navigationEntry = entityEntry.Navigation(Member);
            if (!navigationEntry.IsLoaded && entityEntry.State != EntityState.Detached)
                navigationEntry.Load();

            return navigationEntry.GetCurrentValue();
        }
    }

    public override async Task CollectChangesAsync(DbContext dbContext, EFCoreChangeset changes)
    {
        foreach (var entityEntry in dbContext.EntityEntriesOfType(Member.DeclaringEntityType))
        {
            await CollectChangesAsync(entityEntry, changes);
        }
        if (Member.Inverse is not null)
        {
            foreach (var entityEntry in dbContext.EntityEntriesOfType(Member.Inverse.DeclaringEntityType))
            {
                await CollectChangesAsync(entityEntry, changes);
            }
        }
        if (Member is ISkipNavigation skipNavigation)
        {
            foreach (var joinEntry in dbContext.EntityEntriesOfType(skipNavigation.JoinEntityType))
            {
                await CollectChangesAsync(joinEntry, changes);
            }
        }
    }

    public override async Task CollectChangesAsync(EntityEntry entityEntry, EFCoreChangeset changes)
    {
        if (entityEntry.State != EntityState.Added
            && entityEntry.State != EntityState.Deleted
            && entityEntry.State != EntityState.Modified)
        {
            return;
        }

        if (Member.DeclaringEntityType.IsAssignableFrom(entityEntry.Metadata))
        {
            var navigationEntry = entityEntry.Navigation(Member);
            if (entityEntry.State == EntityState.Added
                || entityEntry.State == EntityState.Deleted
                || navigationEntry.IsModified)
            {
                var modifiedEntities = navigationEntry.GetModifiedEntities();

                foreach (var ent in modifiedEntities.added)
                    changes.RegisterNavigationAdded(Member, entityEntry.Entity, ent);

                foreach (var ent in modifiedEntities.removed)
                    changes.RegisterNavigationRemoved(Member, entityEntry.Entity, ent);
            }
        }

        if (Member.Inverse is not null && Member.Inverse.DeclaringEntityType.IsAssignableFrom(entityEntry.Metadata))
        {
            if (entityEntry.State == EntityState.Added
                                || entityEntry.State == EntityState.Deleted
                                || entityEntry.State == EntityState.Modified)
            {
                var inverseNavigationEntry = entityEntry.Navigation(Member.Inverse);
                if (entityEntry.State == EntityState.Added
                    || entityEntry.State == EntityState.Deleted
                    || inverseNavigationEntry.IsModified)
                {
                    if (!inverseNavigationEntry.IsLoaded && entityEntry.State != EntityState.Detached)
                        await inverseNavigationEntry.LoadAsync();

                    var modifiedEntities = inverseNavigationEntry.GetModifiedEntities();

                    foreach (var entity in modifiedEntities.added)
                        changes.RegisterNavigationAdded(Member, entity, entityEntry.Entity);

                    foreach (var entity in modifiedEntities.removed)
                        changes.RegisterNavigationRemoved(Member, entity, entityEntry.Entity);
                }
            }
        }

        if (Member is ISkipNavigation skipNavigation && skipNavigation.JoinEntityType.IsAssignableFrom(entityEntry.Metadata))
        {
            await CollectJoinEntryChangesAsync(skipNavigation, entityEntry, changes);
        }
    }

    private async Task CollectJoinEntryChangesAsync(
        ISkipNavigation skipNavigation,
        EntityEntry joinEntry,
        EFCoreChangeset changes)
    {
        var dependentToPrincipal = skipNavigation.ForeignKey.DependentToPrincipal;
        var joinReferenceToOther = skipNavigation.Inverse?.ForeignKey.DependentToPrincipal;

        // Join entities are not required to have navigations back to the principals
        if (dependentToPrincipal is null || joinReferenceToOther is null)
        {
            await CollectJoinEntryChangesFromForeignKeysAsync(skipNavigation, joinEntry, changes);
            return;
        }

        var dependentToPrincipalEntry = joinEntry.Navigation(dependentToPrincipal);
        var otherReferenceEntry = joinEntry.Reference(joinReferenceToOther);

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
                        changes.RegisterNavigationAdded(Member, entity, otherEntity);
                }
            }

            if (joinEntry.State == EntityState.Deleted
                || dependentToPrincipalEntry.IsModified)
            {
                foreach (var entity in dependentToPrincipalEntry.GetOriginalEntities())
                {
                    foreach (var otherEntity in otherReferenceEntry.GetOriginalEntities())
                        changes.RegisterNavigationRemoved(Member, entity, otherEntity);
                }
            }
        }
    }

    private async Task CollectJoinEntryChangesFromForeignKeysAsync(
        ISkipNavigation skipNavigation,
        EntityEntry joinEntry,
        EFCoreChangeset changes)
    {
        if (skipNavigation.Inverse is null)
            return;

        var toPrincipalForeignKey = skipNavigation.ForeignKey;
        var toOtherForeignKey = skipNavigation.Inverse.ForeignKey;

        if (joinEntry.State == EntityState.Added
            || joinEntry.State == EntityState.Modified)
        {
            var entity = await FindPrincipalAsync(joinEntry, toPrincipalForeignKey, joinEntry.CurrentValues);
            var otherEntity = await FindPrincipalAsync(joinEntry, toOtherForeignKey, joinEntry.CurrentValues);

            if (entity is not null && otherEntity is not null)
                changes.RegisterNavigationAdded(Member, entity, otherEntity);
        }

        if (joinEntry.State == EntityState.Deleted
            || joinEntry.State == EntityState.Modified)
        {
            var entity = await FindPrincipalAsync(joinEntry, toPrincipalForeignKey, joinEntry.OriginalValues);
            var otherEntity = await FindPrincipalAsync(joinEntry, toOtherForeignKey, joinEntry.OriginalValues);

            if (entity is not null && otherEntity is not null)
                changes.RegisterNavigationRemoved(Member, entity, otherEntity);
        }
    }

    private static async Task<object?> FindPrincipalAsync(
        EntityEntry joinEntry,
        IForeignKey foreignKey,
        PropertyValues joinValues)
    {
        // Principal keys are stable, so tracked principals are matched on their current values
        foreach (var principalEntry in joinEntry.Context.EntityEntriesOfType(foreignKey.PrincipalEntityType))
        {
            if (foreignKey.IsConnected(principalEntry.CurrentValues, joinValues))
                return principalEntry.Entity;
        }

        if (!foreignKey.PrincipalKey.IsPrimaryKey())
            return null;

        // Load principals that are not tracked yet, like LoadAsync does for the navigation above
        var keyValues = new object?[foreignKey.Properties.Count];
        for (var i = 0; i < foreignKey.Properties.Count; i++)
        {
            var keyValue = joinValues[foreignKey.Properties[i]];
            if (keyValue is null)
                return null;

            keyValues[i] = keyValue;
        }

        return await joinEntry.Context.FindAsync(foreignKey.PrincipalEntityType.ClrType, keyValues);
    }

    public async Task<IReadOnlyList<ObservedNavigationChange>> GetChangesAsync(ComputedInput input)
    {
        return input.Get<EFCoreChangeset>().GetChanges(Member);
    }
}

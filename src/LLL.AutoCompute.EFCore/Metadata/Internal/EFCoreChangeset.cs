using System.Collections.Concurrent;
using System.Collections.Frozen;
using LLL.AutoCompute.EFCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore;

public class EFCoreChangeset
{
    public int Count { get; private set; } = 0;
    private readonly ConcurrentDictionary<IProperty, ConcurrentDictionary<object, ObservedPropertyChange>> _propertyChanges = [];
    private readonly ConcurrentDictionary<INavigationBase, ConcurrentDictionary<object, ObservedNavigationChange>> _navigationChanges = [];
    private readonly ConcurrentDictionary<IEntityType, ObservedEntityTypeChange> _entityTypeChanges = [];

    public void RegisterEntityAdded(IEntityType entityType, object entity)
    {
        var entityChange = _entityTypeChanges.GetOrAdd(entityType, static k => new ObservedEntityTypeChange());
        // TODO: Change count
        entityChange.RegisterAdded(entity);
    }

    public void RegisterEntityRemoved(IEntityType entityType, object entity)
    {
        var entityChange = _entityTypeChanges.GetOrAdd(entityType, static k => new ObservedEntityTypeChange());
        // TODO: Change count
        entityChange.RegisterRemoved(entity);
    }

    public void RegisterPropertyChange(IProperty property, object entity, object? originalValue, object? currentValue)
    {
        var propertyChanges = _propertyChanges.GetOrAdd(property, static k => []);

        if (!propertyChanges.TryGetValue(entity, out var change))
        {
            Count++;
            change = new ObservedPropertyChange(entity, originalValue, currentValue);
            propertyChanges[entity] = change;
        }
        else
        {
            change.CurrentValue = currentValue;
        }

        MaybeCleanupPropertyChange(property, propertyChanges, change);
    }

    private void MaybeCleanupPropertyChange(
        IProperty property,
        ConcurrentDictionary<object, ObservedPropertyChange> propertyChanges,
        ObservedPropertyChange change)
    {
        var valueComparer = property.GetValueComparer();
        if (valueComparer.Equals(change.OriginalValue, change.CurrentValue))
        {
            Count--;
            propertyChanges.TryRemove(change.Entity, out _);
        }
    }

    public void RegisterNavigationAdded(INavigationBase navigation, object entity, object relatedEntity)
    {
        var navigationChanges = _navigationChanges.GetOrAdd(navigation, static k => []);
        if (!navigationChanges.TryGetValue(entity, out var navigationChange))
        {
            Count++;
            navigationChange = new ObservedNavigationChange(entity);
            navigationChanges[entity] = navigationChange;
        }
        navigationChange.RegisterAdded(relatedEntity);
        MaybeCleanupNavigationChange(navigation, navigationChanges, navigationChange);
    }

    public void RegisterNavigationRemoved(INavigationBase navigation, object entity, object relatedEntity)
    {
        var navigationChanges = _navigationChanges.GetOrAdd(navigation, static k => []);
        if (!navigationChanges.TryGetValue(entity, out var navigationChange))
        {
            Count++;
            navigationChange = new ObservedNavigationChange(entity);
            navigationChanges[entity] = navigationChange;
        }
        navigationChange.RegisterRemoved(relatedEntity);
        MaybeCleanupNavigationChange(navigation, navigationChanges, navigationChange);
    }

    private void MaybeCleanupNavigationChange(
        INavigationBase navigation,
        ConcurrentDictionary<object, ObservedNavigationChange> navigationChanges,
        ObservedNavigationChange navigationChange)
    {
        if (navigationChange.IsEmpty)
        {
            Count--;
            navigationChanges.TryRemove(navigationChange.Entity, out _);
        }
    }

    public IReadOnlyList<ObservedPropertyChange> GetChanges(IProperty property)
    {
        if (!_propertyChanges.TryGetValue(property, out var changes))
            return [];

        return changes.Values.ToArray();
    }

    public IReadOnlyList<ObservedNavigationChange> GetChanges(INavigationBase navigation)
    {
        if (!_navigationChanges.TryGetValue(navigation, out var changes))
            return [];

        return changes.Values.ToArray();
    }

    public ObservedEntityTypeChange? GetChanges(IEntityType entityType)
    {
        if (!_entityTypeChanges.TryGetValue(entityType, out var changes))
            return null;

        return changes;
    }

    public ObservedPropertyChange? GetChange(IProperty property, object entity)
    {
        if (!_propertyChanges.TryGetValue(property, out var changes))
            return null;

        if (!changes.TryGetValue(entity, out var change))
            return null;

        return change;
    }

    public ObservedNavigationChange? GetChange(INavigationBase navigation, object entity)
    {
        if (!_navigationChanges.TryGetValue(navigation, out var changes))
            return null;

        if (!changes.TryGetValue(entity, out var change))
            return null;

        return change;
    }

    public void MergeInto(EFCoreChangeset target, bool detectCycles)
    {
        foreach (var (member, entities) in _propertyChanges)
        {
            foreach (var (entity, change) in entities)
            {
                if (detectCycles && target.GetChange(member, entity) is not null)
                    throw new Exception($"Cyclic update detected for member: {member.DeclaringType.Name}.{member.Name}");

                target.RegisterPropertyChange(member, entity, change.OriginalValue, change.CurrentValue);
            }
        }

        foreach (var (navigation, observedNavigationChanges) in _navigationChanges)
        {
            foreach (var (entity, entityChanges) in observedNavigationChanges)
            {
                if (detectCycles && target.GetChange(navigation, entity) is not null)
                    throw new Exception($"Cyclic update detected for member: {navigation.DeclaringType.Name}.{navigation.Name}");

                foreach (var added in entityChanges.Added)
                    target.RegisterNavigationAdded(navigation, entity, added);

                foreach (var removed in entityChanges.Removed)
                    target.RegisterNavigationAdded(navigation, entity, removed);
            }
        }

        foreach (var (entityType, entityChanges) in _entityTypeChanges)
        {
            foreach (var added in entityChanges.Added)
                target.RegisterEntityAdded(entityType, added);

            foreach (var removed in entityChanges.Removed)
                target.RegisterEntityRemoved(entityType, removed);
        }
    }

    public EFCoreChangeset DeltaFrom(EFCoreChangeset previous)
    {
        var delta = new EFCoreChangeset();

        var properties = _propertyChanges.Keys.Concat(previous._propertyChanges.Keys).Distinct();
        foreach (var property in properties)
        {
            var comparer = property.GetValueComparer();

            var currentEntities = _propertyChanges.ContainsKey(property)
                ? _propertyChanges[property].Keys
                : [];

            var previousEntities = previous._propertyChanges.ContainsKey(property)
                ? previous._propertyChanges[property].Keys
                : [];

            var entities = currentEntities.Concat(previousEntities).Distinct();

            foreach (var entity in entities)
            {
                var previousChange = previous.GetChange(property, entity);
                var change = GetChange(property, entity);
                var previousValue = previousChange?.CurrentValue;
                var currentValue = change?.CurrentValue;
                if (!comparer.Equals(previousValue, currentValue))
                    delta.RegisterPropertyChange(property, entity, previousValue, currentValue);
            }
        }

        var navigations = _navigationChanges.Keys.Concat(previous._navigationChanges.Keys).Distinct();
        foreach (var navigation in navigations)
        {
            var currentEntities = _navigationChanges.ContainsKey(navigation)
                ? _navigationChanges[navigation].Keys
                : [];

            var previousEntities = previous._navigationChanges.ContainsKey(navigation)
                ? previous._navigationChanges[navigation].Keys
                : [];

            var entities = currentEntities.Concat(previousEntities).Distinct();

            foreach (var entity in entities)
            {
                var previousChange = previous.GetChange(navigation, entity);
                var change = GetChange(navigation, entity);

                var deltaAdded = (change?.Added ?? FrozenSet<object>.Empty).Except(previousChange?.Added ?? FrozenSet<object>.Empty)
                        .Concat((previousChange?.Removed ?? FrozenSet<object>.Empty).Except(change?.Removed ?? FrozenSet<object>.Empty))
                        .ToHashSet();

                foreach (var added in deltaAdded)
                    delta.RegisterNavigationAdded(navigation, entity, added);

                var deltaRemoved = (change?.Removed ?? FrozenSet<object>.Empty).Except(previousChange?.Removed ?? FrozenSet<object>.Empty)
                        .Concat((previousChange?.Added ?? FrozenSet<object>.Empty).Except(change?.Added ?? FrozenSet<object>.Empty))
                        .ToHashSet();

                foreach (var removed in deltaRemoved)
                    delta.RegisterNavigationRemoved(navigation, entity, removed);
            }
        }

        var entityTypes = _entityTypeChanges.Keys.Concat(previous._entityTypeChanges.Keys).Distinct();
        foreach (var entityType in entityTypes)
        {
            var previousChange = previous.GetChanges(entityType);
            var change = GetChanges(entityType);

            var deltaAdded = (change?.Added ?? FrozenSet<object>.Empty).Except(previousChange?.Added ?? FrozenSet<object>.Empty)
                .Concat((previousChange?.Removed ?? FrozenSet<object>.Empty).Except(change?.Removed ?? FrozenSet<object>.Empty))
                .ToHashSet();

            foreach (var added in deltaAdded)
                delta.RegisterEntityAdded(entityType, added);

            var deltaRemoved = (change?.Removed ?? FrozenSet<object>.Empty).Except(previousChange?.Removed ?? FrozenSet<object>.Empty)
                .Concat((previousChange?.Added ?? FrozenSet<object>.Empty).Except(change?.Added ?? FrozenSet<object>.Empty))
                .ToHashSet();

            foreach (var removed in deltaRemoved)
                delta.RegisterEntityRemoved(entityType, removed);
        }

        return delta;
    }

    public IReadOnlySet<ComputedMember> GetAffectedComputedMembers(IReadOnlySet<ComputedMember> targetComputeds)
    {
        var affectedComputedMembers = new HashSet<ComputedMember>();

        var affectedMembers = _propertyChanges.Keys.OfType<IPropertyBase>()
            .Concat(_navigationChanges.Keys);

        foreach (var member in affectedMembers)
        {
            var observedMember = member.GetObservedMember();
            if (observedMember is null)
                continue;

            affectedComputedMembers.UnionWith(observedMember.DependentMembers);
        }

        affectedComputedMembers.IntersectWith(targetComputeds);

        return affectedComputedMembers;
    }
}

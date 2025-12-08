using System.Collections.Concurrent;
using LLL.AutoCompute.EFCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore;

public class EFCoreChangeset
{
    private int _count = 0;

    public int Count => _count;

    private readonly ConcurrentDictionary<IProperty, ObservedPropertyChanges> _propertyChanges = [];
    private readonly ConcurrentDictionary<INavigationBase, ObservedNavigationChanges> _navigationChanges = [];

    public bool AddPropertyChange(IProperty property, object entity)
    {
        _count++;
        return GetOrCreatePropertyChanges(property).RegisterChange(entity);
    }

    public ObservedPropertyChanges GetOrCreatePropertyChanges(IProperty property)
    {
        return _propertyChanges.GetOrAdd(property, static k => new ObservedPropertyChanges());
    }

    public bool RegisterNavigationAdded(INavigationBase navigation, object entity, object relatedEntity)
    {
        _count++;
        return GetOrCreateNavigationChanges(navigation).RegisterAdded(entity, relatedEntity);
    }

    public bool RegisterNavigationRemoved(INavigationBase navigation, object entity, object relatedEntity)
    {
        _count++;
        return GetOrCreateNavigationChanges(navigation).RegisterRemoved(entity, relatedEntity);
    }

    public ObservedNavigationChanges GetOrCreateNavigationChanges(INavigationBase navigation)
    {
        return _navigationChanges.GetOrAdd(navigation, static k => new ObservedNavigationChanges());
    }

    public void MergeInto(EFCoreChangeset target, bool detectCycles)
    {
        foreach (var (member, entities) in _propertyChanges)
        {
            foreach (var entity in entities.GetEntityChanges())
            {
                if (!target.AddPropertyChange(member, entity) && detectCycles)
                    throw new Exception($"Cyclic update detected for member: {member.DeclaringType.Name}.{member.Name}");
            }
        }

        foreach (var (navigation, observedNavigationChanges) in _navigationChanges)
        {
            foreach (var (entity, entityChanges) in observedNavigationChanges.GetEntityChanges())
            {
                if (detectCycles && target.GetOrCreateNavigationChanges(navigation).HasEntityChange(entity))
                    throw new Exception($"Cyclic update detected for member: {navigation.DeclaringType.Name}.{navigation.Name}");

                foreach (var added in entityChanges.Added)
                    target.RegisterNavigationAdded(navigation, entity, added);

                foreach (var removed in entityChanges.Removed)
                    target.RegisterNavigationAdded(navigation, entity, removed);
            }
        }
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

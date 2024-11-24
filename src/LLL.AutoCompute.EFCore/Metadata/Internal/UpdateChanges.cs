using System.Collections.Concurrent;
using LLL.AutoCompute.EFCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore;

public class UpdateChanges
{
    private int _count = 0;

    public int Count => _count;

    private readonly ConcurrentDictionary<IPropertyBase, HashSet<object>> _memberChanges = [];

    public bool AddMemberChange(IPropertyBase member, object entity)
    {
        _count++;
        var entities = _memberChanges.GetOrAdd(member, k => []);
        return entities.Add(entity);
    }

    public bool AddCreatedEntity(IEntityType type, object entity)
    {
        _count++;
        var added = true;
        foreach (var member in type.GetMembers())
        {
            var entities = _memberChanges.GetOrAdd(member, k => []);
            if (!entities.Add(entity))
                added = false;
        }
        return added;
    }

    public void MergeIntoAndDetectCycles(UpdateChanges target)
    {
        foreach (var (member, entities) in _memberChanges)
        {
            foreach (var entity in entities)
            {
                if (!target.AddMemberChange(member, entity))
                {
                    throw new Exception($"Cyclic update detected for member: {member.DeclaringType.Name}.{member.Name}");
                }
            }
        }
    }

    public IReadOnlySet<ComputedBase> GetAffectedComputeds()
    {
        var affectedComputeds = new HashSet<ComputedBase>();
        foreach (var member in _memberChanges.Keys)
        {
            var observedMember = member.GetObservedMember();
            if (observedMember is null)
                continue;

            foreach (var dependent in observedMember.Dependents)
            {
                affectedComputeds.Add(dependent);
            }
        }
        return affectedComputeds;
    }
}
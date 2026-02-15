using System.Linq.Expressions;

namespace LLL.AutoCompute.EntityContexts;

public class ChangeTrackingEntityContext : EntityContext
{
    public ChangeTrackingEntityContext(
        Expression expression,
        EntityContext parent,
        bool isTrackingChanges)
        : base(expression, [parent])
    {
        EntityType = parent.EntityType;
        IsTrackingChanges = isTrackingChanges;
    }

    public override IObservedEntityType EntityType { get; }
    public override bool IsTrackingChanges { get; }

    public override string ToDebugString() => $"ChangeTracking({EntityType.Name}, {IsTrackingChanges})";

    public override async Task<IReadOnlyCollection<object>> GetParentAffectedEntities(ComputedInput input)
    {
        return await GetAffectedEntitiesAsync(input);
    }
}


using System.Linq.Expressions;

namespace LLL.AutoCompute.EntityContexts;

public class DistinctEntityContext : EntityContext
{
    public DistinctEntityContext(Expression expression, EntityContext parent)
        : base(expression, [parent])
    {
        EntityType = parent.EntityType;
        IsTrackingChanges = parent.IsTrackingChanges;
    }

    public override IObservedEntityType EntityType { get; }
    public override bool IsTrackingChanges { get; }

    public override string ToDebugString() => $"Distinct({EntityType.Name})";

    public override async Task<IReadOnlyCollection<object>> GetParentAffectedEntities(ComputedInput input)
    {
        return await GetAffectedEntitiesAsync(input);
    }

    public override async Task EnrichIncrementalContextAsync(ComputedInput input, IReadOnlyCollection<object> entities)
    {
        await base.EnrichIncrementalContextAsync(input, entities);
        await EnrichIncrementalContextTowardsRootAsync(input, entities);
    }
}

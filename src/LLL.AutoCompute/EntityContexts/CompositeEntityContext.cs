using System.Linq.Expressions;

namespace LLL.AutoCompute.EntityContexts;

public class CompositeEntityContext : EntityContext
{
    public override IObservedEntityType EntityType { get; }
    public override bool IsTrackingChanges { get; }

    public CompositeEntityContext(
        Expression expression,
        IReadOnlyList<EntityContext> parents)
        : base(expression, parents)
    {
        EntityType = parents[0].EntityType;
        IsTrackingChanges = parents.Any(c => c.IsTrackingChanges);
    }

    public override async Task<IReadOnlyCollection<object>> GetParentAffectedEntities(ComputedInput input)
    {
        return await GetAffectedEntitiesAsync(input);
    }
}

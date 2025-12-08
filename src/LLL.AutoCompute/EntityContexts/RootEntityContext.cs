using System.Linq.Expressions;

namespace LLL.AutoCompute.EntityContexts;

public class RootEntityContext(
    Expression expression,
    IObservedEntityType entityType)
    : EntityContext(expression, [])
{
    public override IObservedEntityType EntityType => entityType;
    public override bool IsTrackingChanges => true;

    public override async Task<IReadOnlyCollection<object>> GetParentAffectedEntities(ComputedInput input)
    {
        throw new InvalidOperationException("Can't call GetParentAffectedEntities on RootEntityContext");
    }
}

using System.Linq.Expressions;

namespace LLL.AutoCompute.EntityContexts;

public class EmptyEntityContext(
    Expression expression,
    IObservedEntityType entityType)
    : EntityContext(expression, [])
{
    public override IObservedEntityType EntityType => entityType;
    public override bool IsTrackingChanges => false;

    public override async Task<IReadOnlyCollection<object>> GetParentAffectedEntities(object input, IncrementalContext? incrementalContext)
    {
        throw new InvalidOperationException("Can't call GetParentAffectedEntities on EmptyEntityContext");
    }
}


using System.Linq.Expressions;

namespace LLL.AutoCompute.EntityContexts;

public class ScopedEntityContext : EntityContext
{
    private readonly EntityContext _parent;

    public ScopedEntityContext(
        Expression expression,
        EntityContext parent)
        : base(expression, [parent])
    {
        _parent = parent;
        EntityType = parent.EntityType;
        IsTrackingChanges = parent.IsTrackingChanges;
    }


    public override IObservedEntityType EntityType { get; }
    public override bool IsTrackingChanges { get; }

    public override async Task<IReadOnlyCollection<object>> GetParentAffectedEntities(object input, IncrementalContext incrementalContext)
    {
        return await GetAffectedEntitiesAsync(input, incrementalContext);
    }
}

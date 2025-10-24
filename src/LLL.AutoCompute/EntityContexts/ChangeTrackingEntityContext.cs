using System.Linq.Expressions;

namespace LLL.AutoCompute.EntityContexts;

public class ChangeTrackingEntityContext : EntityContext
{
    private readonly EntityContext _parent;

    public ChangeTrackingEntityContext(
        Expression expression,
        EntityContext parent,
        bool trackChanges)
        : base(expression, [parent])
    {
        EntityType = parent.EntityType;
        _parent = parent;
        IsTrackingChanges = trackChanges;
    }

    public override IObservedEntityType EntityType { get; }
    public override bool IsTrackingChanges { get; }

    public override async Task<IReadOnlyCollection<object>> GetParentAffectedEntities(object input, IncrementalContext? incrementalContext)
    {
        return await GetAffectedEntitiesAsync(input, incrementalContext);
    }
}

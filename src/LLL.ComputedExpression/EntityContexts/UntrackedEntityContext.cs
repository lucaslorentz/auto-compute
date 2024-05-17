namespace LLL.ComputedExpression.EntityContexts;

public class UntrackedEntityContext(
    Type inputType,
    Type entityType,
    Type rootEntityType
) : EntityContext
{
    public override Type InputType => inputType;
    public override Type EntityType => entityType;
    public override Type RootEntityType => rootEntityType;
    public override bool IsTrackingChanges => false;

    public override IAffectedEntitiesProvider? GetParentAffectedEntitiesProvider()
    {
        return null;
    }

    public override IReadOnlyCollection<object> GetCascadedIncrementalEntities(object input, IReadOnlyCollection<object> entities, IncrementalContext? incrementalContext)
    {
        return [];
    }
}

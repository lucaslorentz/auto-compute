namespace LLL.ComputedExpression.EntityContexts;

public class RootEntityContext(Type inputType, Type entityType) : EntityContext
{
    public override Type InputType => inputType;
    public override Type EntityType => entityType;
    public override Type RootEntityType => entityType;
    public override bool IsTrackingChanges => true;

    public override IAffectedEntitiesProvider? GetParentAffectedEntitiesProvider()
    {
        return GetAffectedEntitiesProvider();
    }

    public override IReadOnlyCollection<object> GetCascadedIncrementalEntities(object input, IReadOnlyCollection<object> entities, IncrementalContext? incrementalContext)
    {
        return entities;
    }
}

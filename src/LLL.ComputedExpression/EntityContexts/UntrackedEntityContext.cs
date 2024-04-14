using LLL.ComputedExpression.RootEntitiesProviders;

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

    public override IAffectedEntitiesProvider? GetAffectedEntitiesProviderInverse()
    {
        return null;
    }

    public override IRootEntitiesProvider GetOriginalRootEntitiesProvider()
    {
        var closedType = typeof(EmptyRootEntitiesProvider<,,>)
            .MakeGenericType(InputType, RootEntityType, EntityType);

        return (IRootEntitiesProvider)Activator.CreateInstance(closedType)!;
    }

    public override IRootEntitiesProvider GetCurrentRootEntitiesProvider()
    {
        var closedType = typeof(EmptyRootEntitiesProvider<,,>)
            .MakeGenericType(InputType, RootEntityType, EntityType);

        return (IRootEntitiesProvider)Activator.CreateInstance(closedType)!;
    }
}

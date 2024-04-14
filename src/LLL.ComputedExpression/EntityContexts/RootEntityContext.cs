using LLL.ComputedExpression.RootEntitiesProviders;

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

    public override IAffectedEntitiesProvider? GetAffectedEntitiesProviderInverse()
    {
        return null;
    }

    public override IRootEntitiesProvider GetOriginalRootEntitiesProvider()
    {
        var closedType = typeof(NoOpRootEntitiesProvider<,>)
            .MakeGenericType(InputType, EntityType);

        return (IRootEntitiesProvider)Activator.CreateInstance(closedType)!;
    }

    public override IRootEntitiesProvider GetCurrentRootEntitiesProvider()
    {
        var closedType = typeof(NoOpRootEntitiesProvider<,>)
            .MakeGenericType(InputType, EntityType);

        return (IRootEntitiesProvider)Activator.CreateInstance(closedType)!;
    }
}

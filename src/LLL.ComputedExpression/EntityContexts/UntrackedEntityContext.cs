using LLL.ComputedExpression.RootEntitiesProvider;

namespace LLL.ComputedExpression.EntityContexts;

public class UntrackedEntityContext : EntityContext
{
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
        // TODO: Implement
        return new EmptyRootEntitiesProvider();
    }

    public override IRootEntitiesProvider GetCurrentRootEntitiesProvider()
    {
        // TODO: Implement
        return new EmptyRootEntitiesProvider();
    }
}

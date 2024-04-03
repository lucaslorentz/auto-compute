using LLL.ComputedExpression.RootEntitiesProvider;

namespace LLL.ComputedExpression.EntityContexts;

public class RootEntityContext : EntityContext
{
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
        return new NoOpRootEntitiesProvider();
    }

    public override IRootEntitiesProvider GetCurrentRootEntitiesProvider()
    {
        return new NoOpRootEntitiesProvider();
    }
}

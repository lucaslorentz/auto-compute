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

    public override async Task<IReadOnlyCollection<object>> LoadOriginalRootEntities(object input, IReadOnlyCollection<object> entities)
    {
        return [];
    }

    public override async Task<IReadOnlyCollection<object>> LoadCurrentRootEntities(object input, IReadOnlyCollection<object> entities)
    {
        return [];
    }
}

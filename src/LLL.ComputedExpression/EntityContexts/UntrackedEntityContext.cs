
using LLL.Computed.AffectedEntitiesProviders;

namespace LLL.Computed.EntityContexts;

public class UntrackedEntityContext : EntityContext
{
    public override bool IsTrackingChanges => false;

    public override IAffectedEntitiesProvider? GetParentAffectedEntitiesProvider()
    {
        return new EmptyAffectedEntitiesProvider();
    }

    public override IAffectedEntitiesProvider GetAffectedEntitiesProviderInverse()
    {
        return new EmptyAffectedEntitiesProvider();
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

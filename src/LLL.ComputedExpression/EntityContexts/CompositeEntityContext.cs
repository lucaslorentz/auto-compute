using LLL.Computed.AffectedEntitiesProviders;

namespace LLL.Computed.EntityContexts;

public class CompositeEntityContext : EntityContext
{
    private readonly IList<EntityContext> _parents;

    public override bool IsTrackingChanges { get; }

    public CompositeEntityContext(IList<EntityContext> parents)
    {
        IsTrackingChanges = parents.Any(c => c.IsTrackingChanges);

        foreach (var parent in parents)
            parent.RegisterChildContext(this);

        _parents = parents;
    }

    public override IAffectedEntitiesProvider? GetParentAffectedEntitiesProvider()
    {
        return GetAffectedEntitiesProvider();
    }

    public override IAffectedEntitiesProvider? GetAffectedEntitiesProviderInverse()
    {
        var providers = new List<IAffectedEntitiesProvider?>();

        foreach (var parent in _parents)
            providers.Add(parent.GetAffectedEntitiesProviderInverse());

        return AffectedEntitiesProvider.ComposeAndCleanup(providers);
    }

    public override async Task<IReadOnlyCollection<object>> LoadOriginalRootEntities(object input, IReadOnlyCollection<object> objects)
    {
        var rootEntities = new HashSet<object>();
        foreach (var parent in _parents)
        {
            foreach (var rootEntity in await parent.LoadOriginalRootEntities(input, objects))
                rootEntities.Add(rootEntity);
        }
        return rootEntities;
    }

    public override async Task<IReadOnlyCollection<object>> LoadCurrentRootEntities(object input, IReadOnlyCollection<object> objects)
    {
        var rootEntities = new HashSet<object>();
        foreach (var parent in _parents)
        {
            foreach (var rootEntity in await parent.LoadCurrentRootEntities(input, objects))
                rootEntities.Add(rootEntity);
        }
        return rootEntities;
    }
}

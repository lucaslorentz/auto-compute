using System.Runtime.CompilerServices;
using LLL.ComputedExpression.AffectedEntitiesProviders;
using LLL.ComputedExpression.RootEntitiesProvider;

namespace LLL.ComputedExpression.EntityContexts;

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

    public override IRootEntitiesProvider GetOriginalRootEntitiesProvider()
    {
        var providers = new List<IRootEntitiesProvider>();

        foreach (var parent in _parents)
            providers.Add(parent.GetOriginalRootEntitiesProvider());

        return new CompositeRootEntitiesProvider(providers);
    }

    public override IRootEntitiesProvider GetCurrentRootEntitiesProvider()
    {
        var providers = new List<IRootEntitiesProvider>();

        foreach (var parent in _parents)
            providers.Add(parent.GetCurrentRootEntitiesProvider());

        return new CompositeRootEntitiesProvider(providers);
    }
}

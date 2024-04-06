using LLL.ComputedExpression.AffectedEntitiesProviders;
using LLL.ComputedExpression.RootEntitiesProvider;
using LLL.ComputedExpression.Internal;

namespace LLL.ComputedExpression.EntityContexts;

public class CompositeEntityContext : EntityContext
{
    private readonly IList<EntityContext> _parents;

    public override Type InputType { get; }
    public override Type EntityType { get; }
    public override Type RootEntityType { get; }
    public override bool IsTrackingChanges { get; }

    public CompositeEntityContext(IList<EntityContext> parents)
    {
        InputType = parents[0].InputType;
        EntityType = parents[0].EntityType;
        RootEntityType = parents[0].RootEntityType;
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

        var providerType = typeof(IRootEntitiesProvider<,,>)
            .MakeGenericType(InputType, RootEntityType, EntityType);

        var convertedProviders = providers.ToArray(providerType);

        var closedType = typeof(CompositeRootEntitiesProvider<,,>)
            .MakeGenericType(InputType, RootEntityType, EntityType);

        return (IRootEntitiesProvider)Activator.CreateInstance(closedType, [convertedProviders])!;
    }

    public override IRootEntitiesProvider GetCurrentRootEntitiesProvider()
    {
        var providers = new List<IRootEntitiesProvider>();

        foreach (var parent in _parents)
            providers.Add(parent.GetCurrentRootEntitiesProvider());

        var providerType = typeof(IRootEntitiesProvider<,,>)
            .MakeGenericType(InputType, RootEntityType, EntityType);

        var convertedProviders = providers.ToArray(providerType);

        var closedType = typeof(CompositeRootEntitiesProvider<,,>)
            .MakeGenericType(InputType, RootEntityType, EntityType);

        return (IRootEntitiesProvider)Activator.CreateInstance(closedType, [convertedProviders])!;
    }
}

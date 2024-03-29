using System.Linq.Expressions;
using LLL.Computed.AffectedEntitiesProviders;

namespace LLL.Computed.EntityContexts;

public class FilteredEntityContext : EntityContext
{
    private readonly EntityContext _parent;
    private readonly EntityContext _lambdaEntityContext;
    private readonly Delegate _originalFilter;
    private readonly Delegate _currentFilter;

    public FilteredEntityContext(
        EntityContext parent,
        EntityContext parameterContext,
        LambdaExpression originalFilter,
        LambdaExpression currentFilter)
    {
        _parent = parent;
        _lambdaEntityContext = parameterContext;
        _originalFilter = originalFilter.Compile();
        _currentFilter = currentFilter.Compile();
        IsTrackingChanges = parent.IsTrackingChanges;
        parent.RegisterChildContext(this);
    }

    public override bool IsTrackingChanges { get; }

    public override IAffectedEntitiesProvider GetParentAffectedEntitiesProvider()
    {
        return GetAffectedEntitiesProvider();
    }

    public override IAffectedEntitiesProvider GetAffectedEntitiesProviderInverse()
    {
        return CompositeAffectedEntitiesProvider.ComposeIfNecessary([
            _parent.GetAffectedEntitiesProviderInverse(),
            _lambdaEntityContext.GetAffectedEntitiesProvider()
        ]);
    }

    public override async Task<IReadOnlyCollection<object>> LoadOriginalRootEntities(object input, IReadOnlyCollection<object> entities)
    {
        var entitiesPassing = entities
            .Where(e => (bool)_originalFilter.DynamicInvoke(input, e)!)
            .ToArray();
        return await _parent.LoadOriginalRootEntities(input, entitiesPassing);
    }

    public override async Task<IReadOnlyCollection<object>> LoadCurrentRootEntities(object input, IReadOnlyCollection<object> entities)
    {
        var entitiesPassing = entities
            .Where(e => (bool)_currentFilter.DynamicInvoke(e)!)
            .ToArray();
        return await _parent.LoadCurrentRootEntities(input, entitiesPassing);
    }
}

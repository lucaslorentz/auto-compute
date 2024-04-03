using System.Linq.Expressions;
using LLL.ComputedExpression.AffectedEntitiesProviders;
using LLL.ComputedExpression.RootEntitiesProvider;

namespace LLL.ComputedExpression.EntityContexts;

public class FilteredEntityContext : EntityContext
{
    private readonly EntityContext _parent;
    private readonly EntityContext _parameterContext;
    private readonly LambdaExpression _filterLambda;
    private readonly IComputedExpressionAnalyzer _analyzer;

    public FilteredEntityContext(
        EntityContext parent,
        EntityContext parameterContext,
        LambdaExpression filterLambda,
        IComputedExpressionAnalyzer analyzer)
    {
        _parent = parent;
        _parameterContext = parameterContext;
        _filterLambda = filterLambda;
        _analyzer = analyzer;
        IsTrackingChanges = parent.IsTrackingChanges;
        parent.RegisterChildContext(this);
    }

    public override bool IsTrackingChanges { get; }

    public override IAffectedEntitiesProvider? GetParentAffectedEntitiesProvider()
    {
        return GetAffectedEntitiesProvider();
    }

    public override IAffectedEntitiesProvider? GetAffectedEntitiesProviderInverse()
    {
        return AffectedEntitiesProvider.ComposeAndCleanup([
            _parent.GetAffectedEntitiesProviderInverse(),
            _parameterContext.GetAffectedEntitiesProvider()
        ]);
    }

    public override IRootEntitiesProvider GetOriginalRootEntitiesProvider()
    {
        var filter = _analyzer.GetOriginalValueExpression(_filterLambda);
        return new FilteredRootEntitiesProvider(_parent.GetOriginalRootEntitiesProvider(), filter.Compile());
    }

    public override IRootEntitiesProvider GetCurrentRootEntitiesProvider()
    {
        var filter = _analyzer.GetCurrentValueExpression(_filterLambda);
        return new FilteredRootEntitiesProvider(_parent.GetCurrentRootEntitiesProvider(), filter.Compile());
    }
}

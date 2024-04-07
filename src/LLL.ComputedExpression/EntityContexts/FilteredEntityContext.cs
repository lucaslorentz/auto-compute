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
        InputType = parent.InputType;
        EntityType = parent.EntityType;
        RootEntityType = parent.RootEntityType;
        IsTrackingChanges = parent.IsTrackingChanges;
        parent.RegisterChildContext(this);
    }

    public override Type InputType { get; }
    public override Type EntityType { get; }
    public override Type RootEntityType { get; }
    public override bool IsTrackingChanges { get; }

    public override IAffectedEntitiesProvider? GetParentAffectedEntitiesProvider()
    {
        return GetAffectedEntitiesProvider();
    }

    public override IAffectedEntitiesProvider? GetAffectedEntitiesProviderInverse()
    {
        return AffectedEntitiesProviderExtensions.ComposeAndCleanup([
            _parent.GetAffectedEntitiesProviderInverse(),
            _parameterContext.GetAffectedEntitiesProvider()
        ]);
    }

    public override IRootEntitiesProvider GetOriginalRootEntitiesProvider()
    {
        var filter = _analyzer.GetOriginalValueExpression(_filterLambda);

        var closedType = typeof(FilteredRootEntitiesProvider<,,>)
            .MakeGenericType(InputType, RootEntityType, EntityType);

        return (IRootEntitiesProvider)Activator.CreateInstance(
            closedType,
            [_parent.GetOriginalRootEntitiesProvider(), filter.Compile()])!;
    }

    public override IRootEntitiesProvider GetCurrentRootEntitiesProvider()
    {
        var filter = _analyzer.GetCurrentValueExpression(_filterLambda);

        var closedType = typeof(FilteredRootEntitiesProvider<,,>)
            .MakeGenericType(InputType, RootEntityType, EntityType);

        return (IRootEntitiesProvider)Activator.CreateInstance(
            closedType,
            [_parent.GetCurrentRootEntitiesProvider(), filter.Compile()])!;
    }
}

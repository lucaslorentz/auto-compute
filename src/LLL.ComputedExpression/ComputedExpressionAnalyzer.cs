using System.Linq.Expressions;
using LLL.Computed.EntityContextPropagators;
using LLL.Computed.EntityContexts;
using LLL.Computed.ExpressionVisitors;

namespace LLL.Computed;

public class ComputedExpressionAnalyzer<TInput>
    : IComputedExpressionAnalyzer
{
    private readonly IList<IEntityContextPropagator> _entityContextPropagators = [];
    private readonly IList<IEntityChangeTracker> _entityChangeTrackers = [];

    private ComputedExpressionAnalyzer() { }

    public static ComputedExpressionAnalyzer<TInput> Create()
    {
        return new ComputedExpressionAnalyzer<TInput>();
    }

    public static ComputedExpressionAnalyzer<TInput> CreateWithDefaults()
    {
        return Create().AddDefaults();
    }

    public ComputedExpressionAnalyzer<TInput> AddDefaults()
    {
        return AddEntityContextPropagator(new LinqMethodsEntityContextPropagator())
            .AddEntityContextPropagator(new KeyValuePairEntityContextPropagator())
            .AddEntityContextPropagator(new GroupingEntityContextPropagator());
    }

    public ComputedExpressionAnalyzer<TInput> AddStopTrackingDecision(
        IStopTrackingDecision stopTrackingDecision)
    {
        _entityContextPropagators.Insert(0, new UntrackedEntityContextPropagator<TInput>(
            stopTrackingDecision
        ));
        return this;
    }

    public ComputedExpressionAnalyzer<TInput> AddEntityNavigationProvider(
        IEntityNavigationProvider<TInput> navigationProvider)
    {
        _entityContextPropagators.Add(new NavigationEntityContextPropagator<TInput>(
            navigationProvider
        ));
        return this;
    }

    public ComputedExpressionAnalyzer<TInput> AddEntityContextPropagator(
        IEntityContextPropagator propagator)
    {
        _entityContextPropagators.Add(propagator);
        return this;
    }

    public ComputedExpressionAnalyzer<TInput> AddEntityChangeTracker(
        IEntityChangeTracker<TInput> tracker)
    {
        _entityChangeTrackers.Add(tracker);
        return this;
    }

    public IAffectedEntitiesProvider CreateAffectedEntitiesProvider(LambdaExpression computed)
    {
        var analysis = new ComputedExpressionAnalysis();

        var entityContext = new RootEntityContext<TInput>();
        analysis.AddEntityContextProvider(computed.Parameters[0], (key) => key == EntityContextKeys.None ? entityContext : null);

        new PropagateEntityContextsVisitor(
            _entityContextPropagators,
            analysis
        ).Visit(computed);

        new TrackEntityChangesVisitor(
            _entityChangeTrackers,
            analysis
        ).Visit(computed);

        return entityContext.CompositeAffectedEntitiesProvider;
    }
}

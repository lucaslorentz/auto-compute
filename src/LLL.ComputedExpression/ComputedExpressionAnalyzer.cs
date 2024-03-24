using System.Linq.Expressions;
using LLL.Computed.EntityContextPropagators;
using LLL.Computed.EntityContexts;
using LLL.Computed.ExpressionVisitors;

namespace LLL.Computed;

public class ComputedExpressionAnalyzer<TInput>
    : IComputedExpressionAnalyzer
{
    private readonly IList<IEntityContextPropagator> _entityContextPropagators = [];
    private readonly HashSet<IEntityMemberAccessLocator<IEntityProperty>> _propertyAccessLocators = [];
    private readonly HashSet<IEntityMemberAccessLocator<IEntityNavigation>> _navigationAccessLocators = [];
    private readonly HashSet<IEntityMemberAccessLocator> _memberAccessLocators = [];

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
            .AddEntityContextPropagator(new GroupingEntityContextPropagator())
            .AddEntityContextPropagator(new NavigationEntityContextPropagator(_navigationAccessLocators));
    }

    public ComputedExpressionAnalyzer<TInput> AddStopTrackingDecision(
        IStopTrackingDecision stopTrackingDecision)
    {
        _entityContextPropagators.Insert(0, new UntrackedEntityContextPropagator<TInput>(
            stopTrackingDecision
        ));
        return this;
    }

    public ComputedExpressionAnalyzer<TInput> AddEntityNavigationAccessLocator(
        IEntityMemberAccessLocator<IEntityNavigation, TInput> memberAccessLocator)
    {
        _navigationAccessLocators.Add(memberAccessLocator);
        _memberAccessLocators.Add(memberAccessLocator);
        return this;
    }

    public ComputedExpressionAnalyzer<TInput> AddEntityPropertyAccessLocator(
        IEntityMemberAccessLocator<IEntityProperty, TInput> memberAccessLocator)
    {
        _propertyAccessLocators.Add(memberAccessLocator);
        _memberAccessLocators.Add(memberAccessLocator);
        return this;
    }

    public ComputedExpressionAnalyzer<TInput> AddEntityContextPropagator(
        IEntityContextPropagator propagator)
    {
        _entityContextPropagators.Add(propagator);
        return this;
    }

    public IAffectedEntitiesProvider CreateAffectedEntitiesProvider(LambdaExpression computed)
    {
        var analysis = new ComputedExpressionAnalysis();

        var entityContext = new RootEntityContext();
        analysis.AddEntityContextProvider(computed.Parameters[0], (key) => key == EntityContextKeys.None ? entityContext : null);

        new PropagateEntityContextsVisitor(
            _entityContextPropagators,
            analysis
        ).Visit(computed);

        new CollectEntityMemberAccessesVisitor(
            analysis,
            _memberAccessLocators
        ).Visit(computed);

        return entityContext.GetAffectedEntitiesProvider();
    }

    public LambdaExpression GetOldValueExpression(LambdaExpression computed)
    {
        var inputParameter = Expression.Parameter(typeof(object), "input");

        var newBody = new ChangeToPreviousValueVisitor(
            inputParameter,
            _memberAccessLocators
        ).Visit(computed.Body)!;

        return Expression.Lambda(newBody, [inputParameter, .. computed.Parameters]);
    }
}

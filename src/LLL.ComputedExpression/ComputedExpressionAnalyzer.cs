using System.Linq.Expressions;
using LLL.ComputedExpression.EntityContextPropagators;
using LLL.ComputedExpression.EntityContexts;
using LLL.ComputedExpression.ExpressionVisitors;

namespace LLL.ComputedExpression;

public class ComputedExpressionAnalyzer<TInput> : IComputedExpressionAnalyzer
{
    private readonly IList<IEntityContextPropagator> _entityContextPropagators = [];
    private readonly HashSet<IEntityPropertyAccessLocator> _propertyAccessLocators = [];
    private readonly HashSet<IEntityNavigationAccessLocator> _navigationAccessLocators = [];
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
            .AddEntityContextPropagator(new NavigationEntityContextPropagator(_navigationAccessLocators))
            .AddStopTrackingDecision(new StopTrackingDecision());
    }

    public ComputedExpressionAnalyzer<TInput> AddStopTrackingDecision(
        IStopTrackingDecision stopTrackingDecision)
    {
        _entityContextPropagators.Insert(0, new UntrackedEntityContextPropagator(
            stopTrackingDecision
        ));
        return this;
    }

    public ComputedExpressionAnalyzer<TInput> AddEntityNavigationAccessLocator(
        IEntityNavigationAccessLocator<TInput> memberAccessLocator)
    {
        _navigationAccessLocators.Add(memberAccessLocator);
        _memberAccessLocators.Add(memberAccessLocator);
        return this;
    }

    public ComputedExpressionAnalyzer<TInput> AddEntityPropertyAccessLocator(
        IEntityPropertyAccessLocator<TInput> memberAccessLocator)
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

    public IAffectedEntitiesProvider? CreateAffectedEntitiesProvider(LambdaExpression computed)
    {
        var entityContext = GetEntityContext(computed, computed.Parameters[0], EntityContextKeys.None);
        return entityContext.GetAffectedEntitiesProvider();
    }

    public EntityContext GetEntityContext(
        LambdaExpression computed,
        Expression node,
        string entityContextKey)
    {
        var analysis = new ComputedExpressionAnalysis(this);

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

        return analysis.ResolveEntityContext(node, entityContextKey);
    }

    public LambdaExpression GetOriginalValueExpression(LambdaExpression computed)
    {
        var inputParameter = Expression.Parameter(typeof(object), "input");

        var newBody = new ChangeToPreviousValueVisitor(
            inputParameter,
            _memberAccessLocators
        ).Visit(computed.Body)!;

        return Expression.Lambda(newBody, [inputParameter, .. computed.Parameters]);
    }
}

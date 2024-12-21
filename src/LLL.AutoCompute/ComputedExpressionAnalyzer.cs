using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using LLL.AutoCompute.ChangesProviders;
using LLL.AutoCompute.EntityContextPropagators;
using LLL.AutoCompute.EntityContexts;
using LLL.AutoCompute.Internal;
using LLL.AutoCompute.Internal.ExpressionVisitors;

namespace LLL.AutoCompute;

public class ComputedExpressionAnalyzer<TInput> : IComputedExpressionAnalyzer<TInput>
{
    private readonly IList<IEntityContextPropagator> _entityContextPropagators = [];
    private readonly HashSet<IObservedNavigationAccessLocator> _navigationAccessLocators = [];
    private readonly HashSet<IObservedMemberAccessLocator> _memberAccessLocators = [];
    private readonly IList<Func<LambdaExpression, LambdaExpression>> _expressionModifiers = [];
    private IEntityActionProvider<TInput>? _entityActionProvider;

    public ComputedExpressionAnalyzer<TInput> AddDefaults()
    {
        return AddEntityContextPropagator(new ChangeTrackingEntityContextPropagator())
            .AddEntityContextPropagator(new ConditionalEntityContextPropagator())
            .AddEntityContextPropagator(new ArrayEntityContextPropagator())
            .AddEntityContextPropagator(new ConvertEntityContextPropagator())
            .AddEntityContextPropagator(new LinqMethodsEntityContextPropagator())
            .AddEntityContextPropagator(new KeyValuePairEntityContextPropagator())
            .AddEntityContextPropagator(new GroupingEntityContextPropagator())
            .AddEntityContextPropagator(new NavigationEntityContextPropagator(_navigationAccessLocators));
    }

    public ComputedExpressionAnalyzer<TInput> AddObservedMemberAccessLocator(
        IObservedMemberAccessLocator memberAccessLocator)
    {
        if (memberAccessLocator is IObservedNavigationAccessLocator nav)
            _navigationAccessLocators.Add(nav);

        _memberAccessLocators.Add(memberAccessLocator);
        return this;
    }

    public ComputedExpressionAnalyzer<TInput> AddEntityContextPropagator(
        IEntityContextPropagator propagator)
    {
        _entityContextPropagators.Add(propagator);
        return this;
    }

    public ComputedExpressionAnalyzer<TInput> AddExpressionModifier(Func<LambdaExpression, LambdaExpression> modifier)
    {
        _expressionModifiers.Add(modifier);
        return this;
    }

    public ComputedExpressionAnalyzer<TInput> SetEntityActionProvider(
        IEntityActionProvider<TInput> entityActionProvider)
    {
        _entityActionProvider = entityActionProvider;
        return this;
    }

    public IUnboundChangesProvider<TInput, TEntity, TChange> CreateChangesProvider<TEntity, TValue, TChange>(
        Expression<Func<TEntity, TValue>> computedExpression,
        Expression<Func<TEntity, bool>>? filterExpression,
        IChangeCalculation<TValue, TChange> changeCalculation)
        where TEntity : class
    {
        computedExpression = PrepareComputedExpression(computedExpression);

        var computedEntityContext = GetEntityContext(computedExpression, changeCalculation.IsIncremental);

        var computedValueAccessors = new ComputedValueAccessors<TInput, TEntity, TValue>(
            changeCalculation.IsIncremental
                ? GetIncrementalOriginalValueExpression(computedExpression).Compile()
                : GetOriginalValueExpression(computedExpression).Compile(),
            changeCalculation.IsIncremental
                ? GetIncrementalCurrentValueExpression(computedExpression).Compile()
                : GetCurrentValueExpression(computedExpression).Compile()
        );

        var filterEntityContext = GetEntityContext(filterExpression, false);

        return new UnboundChangesProvider<TInput, TEntity, TValue, TChange>(
            computedExpression,
            computedEntityContext,
            filterExpression.Compile(),
            filterEntityContext,
            RequireEntityActionProvider(),
            changeCalculation,
            computedValueAccessors
        );
    }

    private RootEntityContext GetEntityContext<TEntity, TValue>(
        Expression<Func<TEntity, TValue>> computedExpression,
        bool isIncremental)
        where TEntity : class
    {
        var analysis = new ComputedExpressionAnalysis();

        var rootEntityType = computedExpression.Parameters[0].Type;
        var entityContext = new RootEntityContext(rootEntityType);
        analysis.AddEntityContextProvider(computedExpression.Parameters[0], (key) => key == EntityContextKeys.None ? entityContext : null);

        new PropagateEntityContextsVisitor(
            _entityContextPropagators,
            analysis
        ).Visit(computedExpression);

        new CollectObservedMembersVisitor(
            analysis,
            _memberAccessLocators
        ).Visit(computedExpression);

        if (isIncremental)
            analysis.RunIncrementalActions();

        entityContext.ValidateAll();

        return entityContext;
    }

    private Expression<Func<TInput, IncrementalContext, TEntity, TValue>> GetOriginalValueExpression<TEntity, TValue>(
        Expression<Func<TEntity, TValue>> computedExpression)
    {
        var inputParameter = Expression.Parameter(typeof(TInput), "input");
        var incrementalContextParameter = Expression.Parameter(typeof(IncrementalContext), "incrementalContext");

        var newBody = new ReplaceObservedMemberAccessVisitor(
            _memberAccessLocators,
            memberAccess => memberAccess.CreateOriginalValueExpression(inputParameter)
        ).Visit(computedExpression.Body)!;

        newBody = PrepareComputedOutputExpression(computedExpression.ReturnType, newBody);

        newBody = ReturnDefaultIfEntityActionExpression(
            inputParameter,
            computedExpression.Parameters.First(),
            newBody,
            EntityAction.Create);

        return (Expression<Func<TInput, IncrementalContext, TEntity, TValue>>)Expression.Lambda(newBody, [
            inputParameter,
            incrementalContextParameter,
            .. computedExpression.Parameters
        ]);
    }

    private Expression<Func<TInput, IncrementalContext, TEntity, TValue>> GetCurrentValueExpression<TEntity, TValue>(
        Expression<Func<TEntity, TValue>> computedExpression)
    {
        var inputParameter = Expression.Parameter(typeof(TInput), "input");
        var incrementalContextParameter = Expression.Parameter(typeof(IncrementalContext), "incrementalContext");

        var newBody = new ReplaceObservedMemberAccessVisitor(
            _memberAccessLocators,
            memberAccess => memberAccess.CreateCurrentValueExpression(inputParameter)
        ).Visit(computedExpression.Body)!;

        newBody = PrepareComputedOutputExpression(computedExpression.ReturnType, newBody);

        newBody = ReturnDefaultIfEntityActionExpression(
            inputParameter,
            computedExpression.Parameters.First(),
            newBody,
            EntityAction.Delete);

        return (Expression<Func<TInput, IncrementalContext, TEntity, TValue>>)Expression.Lambda(newBody, [
            inputParameter,
            incrementalContextParameter,
            .. computedExpression.Parameters
        ]);
    }

    private Expression<Func<TInput, IncrementalContext, TEntity, TValue>> GetIncrementalOriginalValueExpression<TEntity, TValue>(
        Expression<Func<TEntity, TValue>> computedExpression)
    {
        var inputParameter = Expression.Parameter(typeof(TInput), "input");
        var incrementalContextParameter = Expression.Parameter(typeof(IncrementalContext), "incrementalContext");

        var newBody = new ReplaceObservedMemberAccessVisitor(
            _memberAccessLocators,
            memberAccess => memberAccess.CreateIncrementalOriginalValueExpression(inputParameter, incrementalContextParameter)
        ).Visit(computedExpression.Body)!;

        newBody = PrepareComputedOutputExpression(computedExpression.ReturnType, newBody);

        newBody = ReturnDefaultIfEntityActionExpression(
            inputParameter,
            computedExpression.Parameters.First(),
            newBody,
            EntityAction.Create);

        return (Expression<Func<TInput, IncrementalContext, TEntity, TValue>>)Expression.Lambda(newBody, [
            inputParameter,
            incrementalContextParameter,
            .. computedExpression.Parameters
        ]);
    }

    private Expression<Func<TInput, IncrementalContext, TEntity, TValue>> GetIncrementalCurrentValueExpression<TEntity, TValue>(
        Expression<Func<TEntity, TValue>> computedExpression)
    {
        var inputParameter = Expression.Parameter(typeof(TInput), "input");
        var incrementalContextParameter = Expression.Parameter(typeof(IncrementalContext), "incrementalContext");

        var newBody = new ReplaceObservedMemberAccessVisitor(
            _memberAccessLocators,
            memberAccess => memberAccess.CreateIncrementalCurrentValueExpression(inputParameter, incrementalContextParameter)
        ).Visit(computedExpression.Body)!;

        newBody = PrepareComputedOutputExpression(computedExpression.ReturnType, newBody);

        newBody = ReturnDefaultIfEntityActionExpression(
            inputParameter,
            computedExpression.Parameters.First(),
            newBody,
            EntityAction.Delete);

        return (Expression<Func<TInput, IncrementalContext, TEntity, TValue>>)Expression.Lambda(newBody, [
            inputParameter,
            incrementalContextParameter,
            .. computedExpression.Parameters
        ]);
    }

    private IEntityActionProvider<TInput> RequireEntityActionProvider()
    {
        return _entityActionProvider
            ?? throw new Exception("Entity Action Provider not configured");
    }
    private Expression<Func<TEntity, TValue>> PrepareComputedExpression<TEntity, TValue>(Expression<Func<TEntity, TValue>> computedExpression) where TEntity : class
    {
        foreach (var modifier in _expressionModifiers)
            computedExpression = (Expression<Func<TEntity, TValue>>)modifier(computedExpression);

        return computedExpression;
    }

    private Expression PrepareComputedOutputExpression(Type returnType, Expression body)
    {
        var prepareOutputMethod = GetType().GetMethod(
            nameof(PrepareComputedOutput),
            BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy)!
            .MakeGenericMethod(returnType);

        return Expression.Call(
            prepareOutputMethod,
            body);
    }

    private Expression ReturnDefaultIfEntityActionExpression(
        ParameterExpression inputParameter,
        ParameterExpression entityParameter,
        Expression expression,
        EntityAction entityAction)
    {
        var entityActionProvider = RequireEntityActionProvider();

        return Expression.Condition(
            Expression.Equal(
                Expression.Call(
                    Expression.Constant(entityActionProvider),
                    "GetEntityAction",
                    [],
                    inputParameter,
                    entityParameter
                ),
                Expression.Constant(entityAction)
            ),
            Expression.Default(expression.Type),
            expression
        );
    }

    private static T PrepareComputedOutput<T>(T value)
    {
        var type = typeof(T);
        if (type.IsConstructedGenericType
            && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            && value is IEnumerable enumerable)
        {
            return (T)(object)enumerable.ToArray(type.GetGenericArguments()[0]);
        }

        return value;
    }
}

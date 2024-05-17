using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using LLL.ComputedExpression.Caching;
using LLL.ComputedExpression.ChangesProviders;
using LLL.ComputedExpression.EntityContextPropagators;
using LLL.ComputedExpression.EntityContexts;
using LLL.ComputedExpression.ExpressionVisitors;
using LLL.ComputedExpression.Internal;

namespace LLL.ComputedExpression;

public class ComputedExpressionAnalyzer<TInput> : IComputedExpressionAnalyzer<TInput>
{
    private readonly IConcurrentCreationCache _concurrentCreationCache;
    private readonly IEqualityComparer<Expression> _expressionEqualityComparer;
    private readonly IList<IEntityContextPropagator> _entityContextPropagators = [];
    private readonly HashSet<IEntityNavigationAccessLocator> _navigationAccessLocators = [];
    private readonly HashSet<IEntityMemberAccessLocator> _memberAccessLocators = [];
    private readonly IList<Func<LambdaExpression, LambdaExpression>> _expressionModifiers = [];
    private IEntityActionProvider<TInput>? _entityActionProvider;

    public ComputedExpressionAnalyzer(
        IConcurrentCreationCache concurrentCreationCache,
        IEqualityComparer<Expression> expressionEqualityComparer)
    {
        _expressionEqualityComparer = expressionEqualityComparer;
        _concurrentCreationCache = concurrentCreationCache;
    }

    public ComputedExpressionAnalyzer<TInput> AddDefaults()
    {
        return AddEntityContextPropagator(new UntrackedEntityContextPropagator<TInput>())
            .AddEntityContextPropagator(new LinqMethodsEntityContextPropagator())
            .AddEntityContextPropagator(new KeyValuePairEntityContextPropagator())
            .AddEntityContextPropagator(new GroupingEntityContextPropagator())
            .AddEntityContextPropagator(new NavigationEntityContextPropagator(_navigationAccessLocators));
    }

    public ComputedExpressionAnalyzer<TInput> AddEntityMemberAccessLocator(
        IEntityMemberAccessLocator<TInput> memberAccessLocator)
    {
        if (memberAccessLocator is IEntityNavigationAccessLocator nav)
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
        IEntityActionProvider<TInput> entityActionProvider
    )
    {
        _entityActionProvider = entityActionProvider;
        return this;
    }

    public IUnboundChangesProvider<TInput, TEntity, TResult>? GetChangesProvider<TEntity, TValue, TResult>(
        Expression<Func<TEntity, TValue>> computedExpression,
        Expression<ChangeCalculationSelector<TValue, TResult>> changeCalculationSelector)
        where TEntity : class
    {
        var key = (
            ComputedExpression: new ExpressionCacheKey(computedExpression, _expressionEqualityComparer),
            ChangeCalculationSelector: new ExpressionCacheKey(changeCalculationSelector, _expressionEqualityComparer)
        );

        return _concurrentCreationCache.GetOrCreate(
            key,
            k => CreateChangesProvider(computedExpression, changeCalculationSelector)
        );
    }

    private IUnboundChangesProvider<TInput, TEntity, TResult>? CreateChangesProvider<TEntity, TValue, TResult>(
        Expression<Func<TEntity, TValue>> computedExpression,
        Expression<ChangeCalculationSelector<TValue, TResult>> changeCalculationSelector)
        where TEntity : class
    {
        var computed = PrepareLambda(computedExpression);

        var entityContext = GetEntityContext(computed, computed.Lambda.Parameters[0], EntityContextKeys.None);

        var affectedEntitiesProvider = (IAffectedEntitiesProvider<TInput, TEntity>)entityContext.GetAffectedEntitiesProvider()!;

        if (affectedEntitiesProvider is null)
            return null;

        var changeCalculation = changeCalculationSelector.Compile()(new ChangeCalculations<TValue>());

        var computedValueAccessors = new ComputedValueAccessors<TInput, TEntity, TValue>(
            GetOriginalValueExpression(computed).Compile(),
            GetCurrentValueExpression(computed).Compile(),
            GetIncrementalOriginalValueExpression(computed).Compile(),
            GetIncrementalCurrentValueExpression(computed).Compile()
        );

        return new UnboundChangesProvider<TInput, TEntity, TValue, TResult>(
            affectedEntitiesProvider,
            changeCalculation,
            computedValueAccessors
        );
    }

    private Expression<Func<TInput, IncrementalContext, TEntity, TValue>> GetOriginalValueExpression<TEntity, TValue>(PreparedLambda<Func<TEntity, TValue>> computed)
    {
        var inputParameter = Expression.Parameter(typeof(TInput), "input");
        var incrementalContextParameter = Expression.Parameter(typeof(IncrementalContext), "incrementalContext");

        var newBody = new ReplaceMemberAccessVisitor(
            _memberAccessLocators,
            memberAccess => memberAccess.CreateOriginalValueExpression(inputParameter)
        ).Visit(computed.Lambda.Body)!;

        newBody = PrepareComputedOutputExpression(computed.Lambda.ReturnType, newBody);

        newBody = ReturnDefaultIfEntityActionExpression(
            inputParameter,
            computed.Lambda.Parameters.First(),
            newBody,
            EntityAction.Create);

        return (Expression<Func<TInput, IncrementalContext, TEntity, TValue>>)Expression.Lambda(newBody, [
            inputParameter,
            incrementalContextParameter,
            .. computed.Lambda.Parameters
        ]);
    }

    private Expression<Func<TInput, IncrementalContext, TEntity, TValue>> GetCurrentValueExpression<TEntity, TValue>(PreparedLambda<Func<TEntity, TValue>> computed)
    {
        var inputParameter = Expression.Parameter(typeof(TInput), "input");
        var incrementalContextParameter = Expression.Parameter(typeof(IncrementalContext), "incrementalContext");

        var newBody = new ReplaceMemberAccessVisitor(
            _memberAccessLocators,
            memberAccess => memberAccess.CreateCurrentValueExpression(inputParameter)
        ).Visit(computed.Lambda.Body)!;

        newBody = PrepareComputedOutputExpression(computed.Lambda.ReturnType, newBody);

        newBody = ReturnDefaultIfEntityActionExpression(
            inputParameter,
            computed.Lambda.Parameters.First(),
            newBody,
            EntityAction.Delete);

        return (Expression<Func<TInput, IncrementalContext, TEntity, TValue>>)Expression.Lambda(newBody, [
            inputParameter,
            incrementalContextParameter,
            .. computed.Lambda.Parameters
        ]);
    }

    private Expression<Func<TInput, IncrementalContext, TEntity, TValue>> GetIncrementalOriginalValueExpression<TEntity, TValue>(
        PreparedLambda<Func<TEntity, TValue>> computed)
    {
        var inputParameter = Expression.Parameter(typeof(TInput), "input");
        var incrementalContextParameter = Expression.Parameter(typeof(IncrementalContext), "incrementalContext");

        var analysis = CreateAnalysis(computed);

        analysis.CreateForcedItems();

        var newBody = new ReplaceMemberAccessVisitor(
            _memberAccessLocators,
            memberAccess => memberAccess.CreateIncrementalOriginalValueExpression(analysis, inputParameter, incrementalContextParameter)
        ).Visit(computed.Lambda.Body)!;

        newBody = PrepareComputedOutputExpression(computed.Lambda.ReturnType, newBody);

        newBody = ReturnDefaultIfEntityActionExpression(
            inputParameter,
            computed.Lambda.Parameters.First(),
            newBody,
            EntityAction.Create);

        return (Expression<Func<TInput, IncrementalContext, TEntity, TValue>>)Expression.Lambda(newBody, [
            inputParameter,
            incrementalContextParameter,
            .. computed.Lambda.Parameters
        ]);
    }

    private Expression<Func<TInput, IncrementalContext, TEntity, TValue>> GetIncrementalCurrentValueExpression<TEntity, TValue>(
        PreparedLambda<Func<TEntity, TValue>> computed)
    {
        var inputParameter = Expression.Parameter(typeof(TInput), "input");
        var incrementalContextParameter = Expression.Parameter(typeof(IncrementalContext), "incrementalContext");

        var analysis = CreateAnalysis(computed);

        analysis.CreateForcedItems();

        var newBody = new ReplaceMemberAccessVisitor(
            _memberAccessLocators,
            memberAccess => memberAccess.CreateIncrementalCurrentValueExpression(analysis, inputParameter, incrementalContextParameter)
        ).Visit(computed.Lambda.Body)!;

        newBody = PrepareComputedOutputExpression(computed.Lambda.ReturnType, newBody);

        newBody = ReturnDefaultIfEntityActionExpression(
            inputParameter,
            computed.Lambda.Parameters.First(),
            newBody,
            EntityAction.Delete);

        return (Expression<Func<TInput, IncrementalContext, TEntity, TValue>>)Expression.Lambda(newBody, [
            inputParameter,
            incrementalContextParameter,
            .. computed.Lambda.Parameters
        ]);
    }

    private EntityContext GetEntityContext<T>(
        PreparedLambda<T> computed,
        Expression node,
        string entityContextKey)
    {
        var analysis = CreateAnalysis(computed);

        return analysis.ResolveEntityContext(node, entityContextKey);
    }

    private IEntityActionProvider<TInput> RequireEntityActionProvider()
    {
        return _entityActionProvider
            ?? throw new Exception("Entity Action Provider not configured");
    }

    private ComputedExpressionAnalysis CreateAnalysis<T>(PreparedLambda<T> computed)
    {
        var rootEntityType = computed.Lambda.Parameters[0].Type;

        var analysis = new ComputedExpressionAnalysis(this, rootEntityType);

        var entityContext = new RootEntityContext(typeof(TInput), rootEntityType);

        analysis.AddEntityContextProvider(computed.Lambda.Parameters[0], (key) => key == EntityContextKeys.None ? entityContext : null);

        new PropagateEntityContextsVisitor(
            _entityContextPropagators,
            analysis
        ).Visit(computed.Lambda);

        new CollectEntityMemberAccessesVisitor(
            analysis,
            _memberAccessLocators
        ).Visit(computed.Lambda);

        return analysis;
    }

    private PreparedLambda<T> PrepareLambda<T>(Expression<T> lambdaExpression)
    {
        foreach (var modifier in _expressionModifiers)
            lambdaExpression = (Expression<T>)modifier(lambdaExpression);

        return new PreparedLambda<T>(lambdaExpression);
    }

    private Expression PrepareComputedOutputExpression(Type returnType, Expression body)
    {
        var prepareOutputMethod = GetType().GetMethod(
            nameof(PrepareComputedOutput),
            BindingFlags.NonPublic | BindingFlags.Static)!
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

    record PreparedLambda<T>(Expression<T> Lambda);
}

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

public class ComputedExpressionAnalyzer<TInput>(
    IConcurrentCreationCache concurrentCreationCache,
    IEqualityComparer<Expression> expressionEqualityComparer
) : IComputedExpressionAnalyzer<TInput>
{
    private readonly IConcurrentCreationCache _concurrentCreationCache = concurrentCreationCache;
    private readonly IEqualityComparer<Expression> _expressionEqualityComparer = expressionEqualityComparer;
    private readonly IList<IEntityContextPropagator> _entityContextPropagators = [];
    private readonly HashSet<IEntityNavigationAccessLocator> _navigationAccessLocators = [];
    private readonly HashSet<IEntityMemberAccessLocator> _memberAccessLocators = [];
    private readonly IList<Func<LambdaExpression, LambdaExpression>> _expressionModifiers = [];
    private IEntityActionProvider<TInput>? _entityActionProvider;

    public ComputedExpressionAnalyzer<TInput> AddDefaults()
    {
        return AddEntityContextPropagator(new UntrackedEntityContextPropagator<TInput>())
            .AddEntityContextPropagator(new ConditionalEntityContextPropagator())
            .AddEntityContextPropagator(new ArrayEntityContextPropagator())
            .AddEntityContextPropagator(new ConvertEntityContextPropagator())
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

    public IUnboundChangesProvider<TInput, TEntity, TChange>? GetChangesProvider<TEntity, TValue, TChange>(
        Expression<Func<TEntity, TValue>> computedExpression,
        Expression<Func<TEntity, bool>>? filterExpression,
        Expression<ChangeCalculationSelector<TValue, TChange>> changeCalculationSelector)
        where TEntity : class
    {
        filterExpression ??= static e => true;

        var key = (
            ComputedExpression: new ExpressionCacheKey(computedExpression, _expressionEqualityComparer),
            filterExpression: new ExpressionCacheKey(filterExpression, _expressionEqualityComparer),
            ChangeCalculationSelector: new ExpressionCacheKey(changeCalculationSelector, _expressionEqualityComparer)
        );

        return _concurrentCreationCache.GetOrCreate(
            key,
            k => CreateChangesProvider(computedExpression, filterExpression, changeCalculationSelector)
        );
    }

    private IUnboundChangesProvider<TInput, TEntity, TChange>? CreateChangesProvider<TEntity, TValue, TChange>(
        Expression<Func<TEntity, TValue>> computedExpression,
        Expression<Func<TEntity, bool>> filterExpression,
        Expression<ChangeCalculationSelector<TValue, TChange>> changeCalculationSelector)
        where TEntity : class
    {
        computedExpression = PrepareComputedExpression(computedExpression);

        var changeCalculation = changeCalculationSelector.Compile()(new ChangeCalculations<TValue>());

        var computedEntityContext = GetEntityContext(computedExpression, changeCalculation.IsIncremental);

        var affectedEntitiesProvider = (IAffectedEntitiesProvider<TInput, TEntity>)computedEntityContext.GetAffectedEntitiesProvider()!;

        if (affectedEntitiesProvider is null)
            return null;

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
            affectedEntitiesProvider,
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

        new CollectEntityMemberAccessesVisitor(
            analysis,
            _memberAccessLocators
        ).Visit(computedExpression);

        if (isIncremental)
            analysis.RunIncrementalActions();

        return entityContext;
    }

    private Expression<Func<TInput, IncrementalContext, TEntity, TValue>> GetOriginalValueExpression<TEntity, TValue>(
        Expression<Func<TEntity, TValue>> computedExpression)
    {
        var inputParameter = Expression.Parameter(typeof(TInput), "input");
        var incrementalContextParameter = Expression.Parameter(typeof(IncrementalContext), "incrementalContext");

        var newBody = new ReplaceMemberAccessVisitor(
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

        var newBody = new ReplaceMemberAccessVisitor(
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

        var newBody = new ReplaceMemberAccessVisitor(
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

        var newBody = new ReplaceMemberAccessVisitor(
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

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
    private IObservedEntityTypeResolver? _entityTypeResolver;

    public ComputedExpressionAnalyzer<TInput> AddDefaults()
    {
        return AddEntityContextPropagator(new ChangeTrackingEntityContextPropagator())
            .AddEntityContextPropagator(new ConditionalEntityContextPropagator())
            .AddEntityContextPropagator(new ArrayEntityContextPropagator())
            .AddEntityContextPropagator(new ConvertEntityContextPropagator())
            .AddEntityContextPropagator(new LinqMethodsEntityContextPropagator(new Lazy<IObservedEntityTypeResolver?>(() => _entityTypeResolver)))
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

    public ComputedExpressionAnalyzer<TInput> SetObservedEntityTypeResolver(
        IObservedEntityTypeResolver entityTypeResolver)
    {
        _entityTypeResolver = entityTypeResolver;
        return this;
    }

    public IUnboundChangesProvider<TInput, TEntity, TChange> CreateChangesProvider<TEntity, TValue, TChange>(
        IObservedEntityType<TInput> entityType,
        Expression<Func<TEntity, TValue>> computedExpression,
        Expression<Func<TEntity, bool>>? filterExpression,
        IChangeCalculation<TValue, TChange> changeCalculation)
        where TEntity : class
    {
        filterExpression ??= static x => true;

        computedExpression = PrepareComputedExpression(computedExpression);

        var computedEntityContext = GetEntityContext(entityType, computedExpression, changeCalculation.IsIncremental);

        var computedValueAccessors = new ComputedValueAccessors<TInput, TEntity, TValue>(
            changeCalculation.IsIncremental
                ? GetIncrementalOriginalValueExpression(entityType, computedExpression).Compile()
                : GetOriginalValueExpression(entityType, computedExpression).Compile(),
            changeCalculation.IsIncremental
                ? GetIncrementalCurrentValueExpression(entityType, computedExpression).Compile()
                : GetCurrentValueExpression(entityType, computedExpression).Compile()
        );

        var filterEntityContext = GetEntityContext(entityType, filterExpression, false);

        return new UnboundChangesProvider<TInput, TEntity, TValue, TChange>(
            computedExpression,
            computedEntityContext,
            filterExpression.Compile(),
            filterEntityContext,
            changeCalculation,
            computedValueAccessors
        );
    }

    private RootEntityContext GetEntityContext<TEntity, TValue>(
        IObservedEntityType entityType,
        Expression<Func<TEntity, TValue>> computedExpression,
        bool isIncremental)
        where TEntity : class
    {
        var analysis = new ComputedExpressionAnalysis();

        var entityContext = new RootEntityContext(entityType);
        analysis.AddContext(computedExpression.Parameters[0], EntityContextKeys.None, entityContext);

        new PropagateEntityContextsVisitor(
            _entityContextPropagators,
            analysis
        ).Visit(computedExpression);

        analysis.RunPropagations();

        new CollectObservedMembersVisitor(
            analysis,
            _memberAccessLocators
        ).Visit(computedExpression);

        analysis.RunActions();

        entityContext.ValidateAll();

        return entityContext;
    }

    private Expression<Func<TInput, IncrementalContext, TEntity, TValue>> GetOriginalValueExpression<TEntity, TValue>(
        IObservedEntityType<TInput> entityType,
        Expression<Func<TEntity, TValue>> computedExpression)
    {
        var inputParameter = Expression.Parameter(typeof(TInput), "input");
        var incrementalContextParameter = Expression.Parameter(typeof(IncrementalContext), "incrementalContext");

        var newBody = new ReplaceObservedMemberAccessVisitor(
            _memberAccessLocators,
            memberAccess => memberAccess.CreateOriginalValueExpression(inputParameter)
        ).Visit(computedExpression.Body)!;

        newBody = PrepareComputedOutputExpression(computedExpression.ReturnType, newBody);

        newBody = ReturnDefaultIfEntityStateExpression(
            entityType,
            inputParameter,
            computedExpression.Parameters.First(),
            newBody,
            ObservedEntityState.Added);

        return (Expression<Func<TInput, IncrementalContext, TEntity, TValue>>)Expression.Lambda(newBody, [
            inputParameter,
            incrementalContextParameter,
            .. computedExpression.Parameters
        ]);
    }

    private Expression<Func<TInput, IncrementalContext, TEntity, TValue>> GetCurrentValueExpression<TEntity, TValue>(
        IObservedEntityType<TInput> entityType,
        Expression<Func<TEntity, TValue>> computedExpression)
    {
        var inputParameter = Expression.Parameter(typeof(TInput), "input");
        var incrementalContextParameter = Expression.Parameter(typeof(IncrementalContext), "incrementalContext");

        var newBody = new ReplaceObservedMemberAccessVisitor(
            _memberAccessLocators,
            memberAccess => memberAccess.CreateCurrentValueExpression(inputParameter)
        ).Visit(computedExpression.Body)!;

        newBody = PrepareComputedOutputExpression(computedExpression.ReturnType, newBody);

        newBody = ReturnDefaultIfEntityStateExpression(
            entityType,
            inputParameter,
            computedExpression.Parameters.First(),
            newBody,
            ObservedEntityState.Removed);

        return (Expression<Func<TInput, IncrementalContext, TEntity, TValue>>)Expression.Lambda(newBody, [
            inputParameter,
            incrementalContextParameter,
            .. computedExpression.Parameters
        ]);
    }

    private Expression<Func<TInput, IncrementalContext, TEntity, TValue>> GetIncrementalOriginalValueExpression<TEntity, TValue>(
        IObservedEntityType<TInput> entityType,
        Expression<Func<TEntity, TValue>> computedExpression)
    {
        var inputParameter = Expression.Parameter(typeof(TInput), "input");
        var incrementalContextParameter = Expression.Parameter(typeof(IncrementalContext), "incrementalContext");

        var newBody = new ReplaceObservedMemberAccessVisitor(
            _memberAccessLocators,
            memberAccess => memberAccess.CreateIncrementalOriginalValueExpression(inputParameter, incrementalContextParameter)
        ).Visit(computedExpression.Body)!;

        newBody = PrepareComputedOutputExpression(computedExpression.ReturnType, newBody);

        newBody = ReturnDefaultIfEntityStateExpression(
            entityType,
            inputParameter,
            computedExpression.Parameters.First(),
            newBody,
            ObservedEntityState.Added);

        return (Expression<Func<TInput, IncrementalContext, TEntity, TValue>>)Expression.Lambda(newBody, [
            inputParameter,
            incrementalContextParameter,
            .. computedExpression.Parameters
        ]);
    }

    private Expression<Func<TInput, IncrementalContext, TEntity, TValue>> GetIncrementalCurrentValueExpression<TEntity, TValue>(
        IObservedEntityType<TInput> entityType,
        Expression<Func<TEntity, TValue>> computedExpression)
    {
        var inputParameter = Expression.Parameter(typeof(TInput), "input");
        var incrementalContextParameter = Expression.Parameter(typeof(IncrementalContext), "incrementalContext");

        var newBody = new ReplaceObservedMemberAccessVisitor(
            _memberAccessLocators,
            memberAccess => memberAccess.CreateIncrementalCurrentValueExpression(inputParameter, incrementalContextParameter)
        ).Visit(computedExpression.Body)!;

        newBody = PrepareComputedOutputExpression(computedExpression.ReturnType, newBody);

        newBody = ReturnDefaultIfEntityStateExpression(
            entityType,
            inputParameter,
            computedExpression.Parameters.First(),
            newBody,
            ObservedEntityState.Removed);

        return (Expression<Func<TInput, IncrementalContext, TEntity, TValue>>)Expression.Lambda(newBody, [
            inputParameter,
            incrementalContextParameter,
            .. computedExpression.Parameters
        ]);
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

    private Expression ReturnDefaultIfEntityStateExpression(
        IObservedEntityType<TInput> entityType,
        ParameterExpression inputParameter,
        ParameterExpression entityParameter,
        Expression expression,
        ObservedEntityState entityState)
    {
        return Expression.Condition(
            Expression.Equal(
                Expression.Call(
                    Expression.Constant(entityType),
                    "GetEntityState",
                    [],
                    inputParameter,
                    entityParameter
                ),
                Expression.Constant(entityState)
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

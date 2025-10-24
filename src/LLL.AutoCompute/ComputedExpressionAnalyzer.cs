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
    private readonly IList<IEntityContextNodeRule> _entityContextRules = [];
    private readonly HashSet<IObservedMemberAccessLocator> _memberAccessLocators = [];
    private readonly IList<Func<Expression, Expression>> _expressionModifiers = [];
    private IObservedEntityTypeResolver? _entityTypeResolver;

    public ComputedExpressionAnalyzer<TInput> AddDefaults()
    {
        return AddEntityContextRule(new ChangeTrackingEntityContextRule())
            .AddEntityContextRule(new ConditionalEntityContextRule())
            .AddEntityContextRule(new ArrayEntityContextRule())
            .AddEntityContextRule(new ConvertEntityContextRule())
            .AddEntityContextRule(new LinqMethodsEntityContextRule(new Lazy<IObservedEntityTypeResolver?>(() => _entityTypeResolver)))
            .AddEntityContextRule(new KeyValuePairEntityContextRule())
            .AddEntityContextRule(new GroupingEntityContextRule())
            .AddEntityContextRule(new MemberEntityContextRule(_memberAccessLocators));
    }

    public ComputedExpressionAnalyzer<TInput> AddObservedMemberAccessLocator(
        IObservedMemberAccessLocator memberAccessLocator)
    {
        _memberAccessLocators.Add(memberAccessLocator);
        return this;
    }

    public ComputedExpressionAnalyzer<TInput> AddEntityContextRule(
        IEntityContextNodeRule rule)
    {
        _entityContextRules.Add(rule);
        return this;
    }

    public ComputedExpressionAnalyzer<TInput> AddExpressionModifier(Func<Expression, Expression> modifier)
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

    public IComputedChangesProvider<TInput, TEntity, TChange> CreateChangesProvider<TEntity, TValue, TChange>(
        IObservedEntityType<TInput> entityType,
        Expression<Func<TEntity, TValue>> computedExpression,
        Expression<Func<TEntity, bool>>? filterExpression,
        IChangeCalculator<TValue, TChange> changeCalculation)
        where TEntity : class
    {
        filterExpression ??= static x => true;

        computedExpression = (Expression<Func<TEntity, TValue>>)RunExpressionModifiers(computedExpression);

        var computedEntityContext = GetEntityContext(entityType, computedExpression, changeCalculation.IsIncremental);

        var originalValueGetter = GetOriginalValueExpression(entityType, computedExpression).Compile();
        var currentValueGetter = GetCurrentValueExpression(entityType, computedExpression).Compile();

        var filterEntityContext = GetEntityContext(entityType, filterExpression, false);

        return new ComputedChangesProvider<TInput, TEntity, TValue, TChange>(
            computedExpression,
            computedEntityContext,
            filterExpression.Compile(),
            filterEntityContext,
            changeCalculation,
            originalValueGetter,
            currentValueGetter
        );
    }

    private RootEntityContext GetEntityContext<TEntity, TValue>(
        IObservedEntityType entityType,
        Expression<Func<TEntity, TValue>> computedExpression,
        bool isIncremental)
        where TEntity : class
    {
        var entityContext = new RootEntityContext(computedExpression.Parameters[0], entityType);

        var entityContextRegistry = new EntityContextRegistry();
        entityContextRegistry.RegisterContext(computedExpression.Parameters[0], EntityContextKeys.None, entityContext);

        new ApplyEntityContextNodeRulesVisitor(
            _entityContextRules,
            entityContextRegistry
        ).Visit(computedExpression);

        entityContextRegistry.PrepareEntityContexts();

        entityContext.ValidateAll();

        return entityContext;
    }

    private Expression<Func<TInput, IncrementalContext?, TEntity, TValue>> GetOriginalValueExpression<TEntity, TValue>(
        IObservedEntityType<TInput> entityType,
        Expression<Func<TEntity, TValue>> computedExpression)
    {
        return GetValueExpression(
            entityType,
            computedExpression,
            (memberAccess, parameters) => memberAccess.CreateOriginalValueExpression(
                parameters.Input,
                parameters.IncrementalContext),
            ObservedEntityState.Added
        );
    }

    private Expression<Func<TInput, IncrementalContext?, TEntity, TValue>> GetCurrentValueExpression<TEntity, TValue>(
        IObservedEntityType<TInput> entityType,
        Expression<Func<TEntity, TValue>> computedExpression)
    {
        return GetValueExpression(
            entityType,
            computedExpression,
            (memberAccess, parameters) => memberAccess.CreateCurrentValueExpression(
                parameters.Input,
                parameters.IncrementalContext),
            ObservedEntityState.Removed
        );
    }

    private Expression<Func<TInput, IncrementalContext?, TEntity, TValue>> GetValueExpression<TEntity, TValue>(
        IObservedEntityType<TInput> entityType,
        Expression<Func<TEntity, TValue>> computedExpression,
        Func<IObservedMemberAccess, (ParameterExpression Input, ParameterExpression IncrementalContext), Expression> expressionModifier,
        ObservedEntityState defaultValueEntityState)
    {
        var inputParameter = Expression.Parameter(typeof(TInput), "input");
        var incrementalContextParameter = Expression.Parameter(typeof(IncrementalContext), "incrementalContext");

        var newBody = new ReplaceObservedMemberAccessVisitor(
            _memberAccessLocators,
            memberAccess => expressionModifier(memberAccess, (inputParameter, incrementalContextParameter))
        ).Visit(computedExpression.Body)!;

        newBody = PrepareComputedOutputExpression(computedExpression.ReturnType, newBody);

        newBody = ReturnDefaultIfEntityStateExpression(
            entityType,
            inputParameter,
            computedExpression.Parameters.First(),
            newBody,
            defaultValueEntityState);

        return (Expression<Func<TInput, IncrementalContext?, TEntity, TValue>>)Expression.Lambda(newBody, [
            inputParameter,
            incrementalContextParameter,
            .. computedExpression.Parameters
        ]);
    }

    public Expression RunExpressionModifiers(Expression expression)
    {
        foreach (var modifier in _expressionModifiers)
            expression = modifier(expression);

        return expression;
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

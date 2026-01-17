using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using LLL.AutoCompute.ChangesProviders;
using LLL.AutoCompute.EntityContextPropagators;
using LLL.AutoCompute.EntityContexts;
using LLL.AutoCompute.Internal;
using LLL.AutoCompute.Internal.ExpressionVisitors;

namespace LLL.AutoCompute;

public class ComputedExpressionAnalyzer : IComputedExpressionAnalyzer
{
    private readonly IList<IEntityContextNodeRule> _entityContextRules = [];
    private readonly HashSet<IObservedMemberAccessLocator> _memberAccessLocators = [];
    private readonly IList<Func<Expression, Expression>> _expressionModifiers = [];
    private readonly IList<Func<Expression, Expression>> _databaseExpressionModifiers = [];
    private IObservedEntityTypeResolver? _entityTypeResolver;

    public ComputedExpressionAnalyzer AddDefaults()
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

    public ComputedExpressionAnalyzer AddObservedMemberAccessLocator(
        IObservedMemberAccessLocator memberAccessLocator)
    {
        _memberAccessLocators.Add(memberAccessLocator);
        return this;
    }

    public ComputedExpressionAnalyzer AddEntityContextRule(
        IEntityContextNodeRule rule)
    {
        _entityContextRules.Add(rule);
        return this;
    }

    public ComputedExpressionAnalyzer AddExpressionModifier(Func<Expression, Expression> modifier)
    {
        _expressionModifiers.Add(modifier);
        return this;
    }

    public ComputedExpressionAnalyzer AddDatabaseExpressionModifier(Func<Expression, Expression> modifier)
    {
        _databaseExpressionModifiers.Add(modifier);
        return this;
    }

    public ComputedExpressionAnalyzer SetObservedEntityTypeResolver(
        IObservedEntityTypeResolver entityTypeResolver)
    {
        _entityTypeResolver = entityTypeResolver;
        return this;
    }

    public IComputedChangesProvider<TEntity, TChange> CreateChangesProvider<TEntity, TValue, TChange>(
        IObservedEntityType entityType,
        Expression<Func<TEntity, TValue>> computedExpression,
        Expression<Func<TEntity, bool>>? filterExpression,
        IChangeCalculator<TValue, TChange> changeCalculator)
        where TEntity : class
    {
        filterExpression ??= static x => true;

        computedExpression = (Expression<Func<TEntity, TValue>>)RunExpressionModifiers(computedExpression);

        var computedEntityContext = GetEntityContext(entityType, computedExpression);

        var originalValueGetter = GetOriginalValueExpression(entityType, computedExpression).Compile();
        var currentValueGetter = GetCurrentValueExpression(entityType, computedExpression).Compile();

        var filterEntityContext = GetEntityContext(entityType, filterExpression);

        return new ComputedChangesProvider<TEntity, TValue, TChange>(
            computedExpression,
            computedEntityContext,
            filterExpression.Compile(),
            filterEntityContext,
            changeCalculator,
            originalValueGetter,
            currentValueGetter
        );
    }

    private RootEntityContext GetEntityContext<TEntity, TValue>(
        IObservedEntityType entityType,
        Expression<Func<TEntity, TValue>> computedExpression)
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

    private Expression<Func<ComputedInput, TEntity, TValue>> GetOriginalValueExpression<TEntity, TValue>(
        IObservedEntityType entityType,
        Expression<Func<TEntity, TValue>> computedExpression)
    {
        return GetValueExpression(
            entityType,
            computedExpression,
            (memberAccess, inputParameter) => memberAccess.Member.CreateOriginalValueExpression(memberAccess, inputParameter),
            ObservedEntityState.Added
        );
    }

    private Expression<Func<ComputedInput, TEntity, TValue>> GetCurrentValueExpression<TEntity, TValue>(
        IObservedEntityType entityType,
        Expression<Func<TEntity, TValue>> computedExpression)
    {
        return GetValueExpression(
            entityType,
            computedExpression,
            (memberAccess, inputParameter) => memberAccess.Member.CreateCurrentValueExpression(memberAccess, inputParameter),
            ObservedEntityState.Removed
        );
    }

    private Expression<Func<ComputedInput, TEntity, TValue>> GetValueExpression<TEntity, TValue>(
        IObservedEntityType entityType,
        Expression<Func<TEntity, TValue>> computedExpression,
        Func<ObservedMemberAccess, ParameterExpression, Expression> expressionModifier,
        ObservedEntityState defaultValueEntityState)
    {
        var inputParameter = Expression.Parameter(typeof(ComputedInput), "input");

        var newBody = new ReplaceObservedMemberAccessVisitor(
            _memberAccessLocators,
            memberAccess => expressionModifier(memberAccess, inputParameter)
        ).Visit(computedExpression.Body)!;

        newBody = PrepareComputedOutputExpression(computedExpression.ReturnType, newBody);

        newBody = ReturnDefaultIfEntityStateExpression(
            entityType,
            inputParameter,
            computedExpression.Parameters.First(),
            newBody,
            defaultValueEntityState);

        return (Expression<Func<ComputedInput, TEntity, TValue>>)Expression.Lambda(newBody, [
            inputParameter,
            .. computedExpression.Parameters
        ]);
    }

    public Expression RunExpressionModifiers(Expression expression)
    {
        foreach (var modifier in _expressionModifiers)
            expression = modifier(expression);

        return expression;
    }

    public Expression RunDatabaseExpressionModifiers(Expression expression)
    {
        foreach (var modifier in _databaseExpressionModifiers)
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
        IObservedEntityType entityType,
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

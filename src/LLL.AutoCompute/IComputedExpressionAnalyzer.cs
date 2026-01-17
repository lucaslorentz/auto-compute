using System.Linq.Expressions;

namespace LLL.AutoCompute;

/// <summary>
/// Analyzes computed expressions and creates change providers for tracking entity changes.
/// </summary>
public interface IComputedExpressionAnalyzer
{
    /// <summary>
    /// Creates a changes provider that tracks changes for entities matching the given expression.
    /// </summary>
    /// <typeparam name="TEntity">The entity type being observed.</typeparam>
    /// <typeparam name="TValue">The computed value type.</typeparam>
    /// <typeparam name="TChange">The change representation type.</typeparam>
    /// <param name="entityType">The observed entity type metadata.</param>
    /// <param name="computedExpression">The expression that computes values from entities.</param>
    /// <param name="filterExpression">An optional filter to limit which entities are tracked.</param>
    /// <param name="changeCalculator">The calculator that determines changes between values.</param>
    /// <returns>A changes provider that detects changes for affected entities.</returns>
    IComputedChangesProvider<TEntity, TChange> CreateChangesProvider<TEntity, TValue, TChange>(
        IObservedEntityType entityType,
        Expression<Func<TEntity, TValue>> computedExpression,
        Expression<Func<TEntity, bool>> filterExpression,
        IChangeCalculator<TValue, TChange> changeCalculator)
        where TEntity : class;

    /// <summary>
    /// Runs all registered expression modifiers on the given expression.
    /// </summary>
    Expression RunExpressionModifiers(Expression expression);

    /// <summary>
    /// Runs all registered database expression modifiers on the given expression.
    /// </summary>
    Expression RunDatabaseExpressionModifiers(Expression expression);
}

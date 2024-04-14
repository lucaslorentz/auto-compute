using LLL.ComputedExpression.ChangesProviders;

namespace LLL.ComputedExpression;

public static class ChangesProviderExtensions
{
    public static IChangesProvider<TInput, TEntity, TValueC> Combine<TInput, TEntity, TValueA, TValueB, TValueC>(
        this IChangesProvider<TInput, TEntity, TValueA> changesProviderA,
        IChangesProvider<TInput, TEntity, TValueB> changesProviderB,
        Func<TValueA, TValueB, TValueC> combineValue
    ) where TEntity : class
    {
        return new CombinedChangesProvider<TInput, TEntity, TValueA, TValueB, TValueC>(
            changesProviderA,
            changesProviderB,
            combineValue
        );
    }

    public static IChangesProvider<TInput, TEntity, TValue> SkipEqualValues<TInput, TEntity, TValue>(
        this IChangesProvider<TInput, TEntity, TValue> changesProvider,
        IEqualityComparer<TValue> valueEqualityComparer
    ) where TEntity : class
    {
        return new SkipEqualsChangesProvider<TInput, TEntity, TValue>(changesProvider, valueEqualityComparer);
    }

    public static IChangesProvider<TInput, TEntity, TValue> CreateContinuedChangesProvider<TInput, TEntity, TValue>(
        this IChangesProvider<TInput, TEntity, TValue> changesProvider
    ) where TEntity : class
    {
        return new ContinuedChangesProvider<TInput, TEntity, TValue>(changesProvider);
    }
}
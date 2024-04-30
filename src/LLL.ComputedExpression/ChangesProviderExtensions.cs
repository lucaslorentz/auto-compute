using LLL.ComputedExpression.ChangesProviders;

namespace LLL.ComputedExpression;

public static class ChangesProviderExtensions
{
    public static IChangesProvider<TInput, TEntity, TValueC> Combine<TInput, TEntity, TValueA, TValueB, TValueC>(
        this IChangesProvider<TInput, TEntity, TValueA> changesProviderA,
        IChangesProvider<TInput, TEntity, TValueB> changesProviderB,
        Func<TValueA, TValueB, TValueC> combineValue,
        IEqualityComparer<TValueC> valueEqualityComparer
    ) where TEntity : class
    {
        return new CombinedChangesProvider<TInput, TEntity, TValueA, TValueB, TValueC>(
            changesProviderA,
            changesProviderB,
            combineValue,
            valueEqualityComparer
        );
    }

    public static IChangesProvider CreateDeltaChangesProvider(
        this IChangesProvider changesProvider)
    {
        var closedType = typeof(DeltaChangesProvider<,,>)
            .MakeGenericType(
                changesProvider.InputType,
                changesProvider.EntityType,
                changesProvider.ValueType);

        return (IChangesProvider)Activator.CreateInstance(
            closedType,
            changesProvider,
            changesProvider.ValueEqualityComparer)!;
    }

    public static IChangesProvider<TInput, TEntity, TValue> CreateDeltaChangesProvider<TInput, TEntity, TValue>(
        this IChangesProvider<TInput, TEntity, TValue> changesProvider)
        where TEntity : class
        where TInput : IDeltaChangesInput
    {
        return new DeltaChangesProvider<TInput, TEntity, TValue>(
            changesProvider,
            changesProvider.ValueEqualityComparer);
    }
}
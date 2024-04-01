using System.Linq.Expressions;

namespace LLL.ComputedExpression.Incremental;

public static class IncrementalComputedExtensions
{
    public static IIncrementalComputed<T, V> AddReference<C, T, V, E>(
        this IIncrementalComputed<T, V> incrementalComputed,
        Expression<Func<T, E>> navigate,
        Expression<Func<E, V>> select)
    {
        incrementalComputed.Parts.Add(
            new IncrementalComputedPart(navigate, select, false)
        );
        return incrementalComputed;
    }

    public static IIncrementalComputed<T, V> AddCollection<T, V, E>(
        this IIncrementalComputed<T, V> incrementalComputed,
        Expression<Func<T, IEnumerable<E>>> navigate,
        Expression<Func<E, V>> extractValue)
    {
        incrementalComputed.Parts.Add(
            new IncrementalComputedPart(navigate, extractValue, true)
        );
        return incrementalComputed;
    }
}
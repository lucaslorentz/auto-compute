using System.Linq.Expressions;
using System.Numerics;

namespace LLL.ComputedExpression.Incremental;

public static class IncrementalComputedBuilderExtensions
{
    public static IncrementalComputedBuilder<T, V> ComposeReference<T, V, E>(
        this IncrementalComputedBuilder<T, V> incrementalComputed,
        Expression<Func<T, E>> navigate,
        Expression<Func<E, V>> select,
        Func<V, V, V, V> update)
    {
        incrementalComputed.Parts.Add(
            new IncrementalComputedPart(navigate, select, update, false)
        );
        return incrementalComputed;
    }

    public static IncrementalComputedBuilder<T, V> ComposeCollection<T, V, E>(
        this IncrementalComputedBuilder<T, V> incrementalComputed,
        Expression<Func<T, IEnumerable<E>>> navigate,
        Expression<Func<E, V>> extractValue,
        Func<V, V, V, V> updater)
    {
        incrementalComputed.Parts.Add(
            new IncrementalComputedPart(navigate, extractValue, updater, true)
        );
        return incrementalComputed;
    }

    public static IncrementalComputedBuilder<T, V> AddReference<T, V, E>(
        this IncrementalComputedBuilder<T, V> incrementalComputed,
        Expression<Func<T, E>> navigate,
        Expression<Func<E, V>> extractValue)
        where V : INumber<V>
    {
        return incrementalComputed.ComposeReference(
            navigate,
            extractValue,
            (c, o, n) => c - o + n
        );
    }

    public static IncrementalComputedBuilder<T, V> AddCollection<T, V, E>(
        this IncrementalComputedBuilder<T, V> incrementalComputed,
        Expression<Func<T, IEnumerable<E>>> navigate,
        Expression<Func<E, V>> extractValue)
        where V : INumber<V>
    {
        return incrementalComputed.ComposeCollection(
            navigate,
            extractValue,
            (c, o, n) => c - o + n
        );
    }
}
using System.Linq.Expressions;

namespace LLL.Computed.Incremental;

public static class IncrementalComputedBuilderExtensions
{
    public static IncrementalComputedBuilder<T, V> Compose<T, V, E>(
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

    public static IncrementalComputedBuilder<T, V> ComposeMany<T, V, E>(
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

    public static IncrementalComputedBuilder<T, int> Add<T, E>(
        this IncrementalComputedBuilder<T, int> incrementalComputed,
        Expression<Func<T, E>> navigate,
        Expression<Func<E, int>> extractValue)
    {
        return incrementalComputed.Compose(
            navigate,
            extractValue,
            (c, o, n) => c - o + n
        );
    }

    public static IncrementalComputedBuilder<T, int> Subtract<T, E>(
        this IncrementalComputedBuilder<T, int> incrementalComputed,
        Expression<Func<T, E>> navigate,
        Expression<Func<E, int>> extractValue)
    {
        return incrementalComputed.Compose(
            navigate,
            extractValue,
            (c, o, n) => c + o - n
        );
    }

    public static IncrementalComputedBuilder<T, int> AddMany<T, E>(
        this IncrementalComputedBuilder<T, int> incrementalComputed,
        Expression<Func<T, IEnumerable<E>>> navigate,
        Expression<Func<E, int>> extractValue)
    {
        return incrementalComputed.ComposeMany(
            navigate,
            extractValue,
            (c, o, n) => c - o + n
        );
    }

    public static IncrementalComputedBuilder<T, int> SubtractMany<T, E>(
        this IncrementalComputedBuilder<T, int> incrementalComputed,
        Expression<Func<T, IEnumerable<E>>> navigate,
        Expression<Func<E, int>> extractValue)
    {
        return incrementalComputed.ComposeMany(
            navigate,
            extractValue,
            (c, o, n) => c + o - n
        );
    }
}
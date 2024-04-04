namespace LLL.ComputedExpression.Incremental;

public interface IIncrementalComputed
{
    object? Zero { get; }
    bool IsZero(object? value);
    object? Add(object? a, object? b);
    object? Remove(object? a, object? b);
    List<IncrementalComputedPart> Parts { get; }
}

public interface IIncrementalComputed<T, V> : IIncrementalComputed
{
    new V Zero { get; }
    bool IsZero(V value);
    V Add(V a, V b);
    V Remove(V a, V b);
    object? IIncrementalComputed.Zero => Zero;
    bool IIncrementalComputed.IsZero(object? value) => value is V v && IsZero(v);
    object? IIncrementalComputed.Add(object? a, object? b)
    {
        if (a is not V aTyped)
            throw new ArgumentException($"Param {nameof(a)} should be of type {typeof(V)}");

        if (b is not V bTyped)
            throw new ArgumentException($"Param {nameof(b)} should be of type {typeof(V)}");

        return Add(aTyped, bTyped);
    }
    object? IIncrementalComputed.Remove(object? a, object? b)
    {
        if (a is not V aTyped)
            throw new ArgumentException($"Param {nameof(a)} should be of type {typeof(V)}");

        if (b is not V bTyped)
            throw new ArgumentException($"Param {nameof(b)} should be of type {typeof(V)}");

        return Remove(aTyped, bTyped);
    }
}

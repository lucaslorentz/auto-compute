namespace LLL.ComputedExpression.Incremental;

public interface IIncrementalComputed
{
    Type EntityType { get; }
    Type ValueType { get; }
    object? Zero { get; }
    bool IsZero(object? value);
    object? Add(object? a, object? b);
    object? Remove(object? a, object? b);
    List<IncrementalComputedPart> Parts { get; }
    object GetValueEqualityComparer();
}

public interface IIncrementalComputed<TEntity, TValue> : IIncrementalComputed
{
    new TValue Zero { get; }
    bool IsZero(TValue value);
    TValue Add(TValue a, TValue b);
    TValue Remove(TValue a, TValue b);
    Type IIncrementalComputed.EntityType => typeof(TEntity);
    Type IIncrementalComputed.ValueType => typeof(TValue);
    object? IIncrementalComputed.Zero => Zero;
    bool IIncrementalComputed.IsZero(object? value) => value is TValue v && IsZero(v);
    object? IIncrementalComputed.Add(object? a, object? b)
    {
        if (a is not TValue aTyped)
            throw new ArgumentException($"Param {nameof(a)} should be of type {typeof(TValue)}");

        if (b is not TValue bTyped)
            throw new ArgumentException($"Param {nameof(b)} should be of type {typeof(TValue)}");

        return Add(aTyped, bTyped);
    }
    object? IIncrementalComputed.Remove(object? a, object? b)
    {
        if (a is not TValue aTyped)
            throw new ArgumentException($"Param {nameof(a)} should be of type {typeof(TValue)}");

        if (b is not TValue bTyped)
            throw new ArgumentException($"Param {nameof(b)} should be of type {typeof(TValue)}");

        return Remove(aTyped, bTyped);
    }
    new IEqualityComparer<TValue> GetValueEqualityComparer() => EqualityComparer<TValue>.Default;
    object IIncrementalComputed.GetValueEqualityComparer() => GetValueEqualityComparer();
}

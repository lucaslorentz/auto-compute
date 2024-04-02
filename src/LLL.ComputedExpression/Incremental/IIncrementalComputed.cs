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
    bool IIncrementalComputed.IsZero(object? value) => IsZero((V)value!);
    object? IIncrementalComputed.Add(object? a, object? b) => Add((V)a!, (V)b!);
    object? IIncrementalComputed.Remove(object? a, object? b) => Remove((V)a!, (V)b!);
}

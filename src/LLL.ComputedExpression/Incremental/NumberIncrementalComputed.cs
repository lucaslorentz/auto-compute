using System.Numerics;

namespace LLL.ComputedExpression.Incremental;

public class NumberIncrementalComputed<T, V> : IIncrementalComputed<T, V>
    where V : INumber<V>
{
    public V Zero { get; } = V.Zero;
    public bool IsZero(V value) => V.IsZero(value);
    public V Add(V a, V b) => a + b;
    public V Remove(V a, V b) => a - b;
    public V Negate(V v) => -v;
    public List<IncrementalComputedPart> Parts { get; } = [];
}

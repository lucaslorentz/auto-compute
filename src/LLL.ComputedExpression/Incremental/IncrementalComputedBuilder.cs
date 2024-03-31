namespace LLL.ComputedExpression.Incremental;

public class IncrementalComputedBuilder<T, V>(V? defaultValue = default)
    : IIncrementalComputed
{
    public V? DefaultValue { get; } = defaultValue;
    public List<IncrementalComputedPart> Parts { get; } = [];

    object? IIncrementalComputed.DefaultValue => DefaultValue;
}

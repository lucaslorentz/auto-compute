namespace LLL.Computed.Incremental;

public interface IIncrementalComputed
{
    object? DefaultValue { get; }
    List<IncrementalComputedPart> Parts { get; }
}

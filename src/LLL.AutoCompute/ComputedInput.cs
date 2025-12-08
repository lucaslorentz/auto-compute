using System.Collections.Concurrent;

namespace LLL.AutoCompute;

public sealed class ComputedInput
{
    private readonly ConcurrentDictionary<object, object?> _values = new();

    public IncrementalContext? IncrementalContext { get; set; }

    public ComputedInput Set<T>(T value)
    {
        _values[typeof(T)] = value;
        return this;
    }

    public bool TryGet<T>(out T? value)
    {
        if (!_values.TryGetValue(typeof(T), out var result))
        {
            value = default;
            return false;
        }
        value = (T)result!;
        return true;
    }

    public T Get<T>()
    {
        if (!_values.TryGetValue(typeof(T), out var result))
            throw new KeyNotFoundException($"No value of type {typeof(T)} found in ComputedInput.");

        return (T)result!;
    }
}

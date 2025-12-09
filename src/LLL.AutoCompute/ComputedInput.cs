using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace LLL.AutoCompute;

public sealed class ComputedInput
{
    private readonly ConcurrentDictionary<object, object?> _values = new();

    public ComputedInput Set<T>(T value)
    {
        _values[typeof(T)] = value;
        return this;
    }

    public void Remove<T>()
    {
        _values.Remove(typeof(T), out var _);
    }

    public bool TryGet<T>([NotNullWhen(true)] out T? value)
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

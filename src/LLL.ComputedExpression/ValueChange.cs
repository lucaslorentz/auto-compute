namespace LLL.ComputedExpression;

public interface IValueChange
{
    object? Original { get; }
    object? Current { get; }
}

public interface IValueChange<TValue> : IValueChange
{
    new TValue Original { get; }
    new TValue Current { get; }
    object? IValueChange.Original => Original;
    object? IValueChange.Current => Current;
}

public class ConstValueChange<TValue>(
    TValue original,
    TValue current
) : IValueChange<TValue>
{
    public TValue Original => original;
    public TValue Current => current;
}


public class LazyValueChange<TValue>(
    Func<TValue> original,
    Func<TValue> current
) : IValueChange<TValue>
{
    private readonly Lazy<TValue> _original = new(original);
    private readonly Lazy<TValue> _current = new(current);

    public TValue Original => _original.Value;
    public TValue Current => _current.Value;
}

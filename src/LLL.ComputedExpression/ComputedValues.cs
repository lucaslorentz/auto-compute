namespace LLL.ComputedExpression;

public class ComputedValues<TValue>(
    Func<TValue> originalValueGetter,
    Func<TValue> currentValueGetter,
    Func<TValue> incrementalOriginalValueGetter,
    Func<TValue> incrementalCurrentValueGetter)
{
    public TValue GetOriginalValue() => originalValueGetter();
    public TValue GetCurrentValue() => currentValueGetter();
    public TValue GetIncrementalOriginalValue() => incrementalOriginalValueGetter();
    public TValue GetIncrementalCurrentValue() => incrementalCurrentValueGetter();
}
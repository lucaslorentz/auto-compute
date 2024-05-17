
namespace LLL.ComputedExpression.ChangeCalculations;

public class ValueChangeCalculation<TValue>(
    IEqualityComparer<TValue> comparer
) : IChangeCalculation<TValue, ValueChange<TValue>>
{
    public bool IsIncremental => false;

    public async Task<ValueChange<TValue>> GetChangeAsync(IComputedValues<TValue> computedValues)
    {
        return new ValueChange<TValue>(
            computedValues.GetOriginalValue(),
            computedValues.GetCurrentValue()
        );
    }
    public ValueChange<TValue> CalculateChange(TValue original, TValue current)
    {
        return new ValueChange<TValue>(
            original,
            current
        );
    }

    public bool IsNoChange(ValueChange<TValue> result)
    {
        return comparer.Equals(result.Original, result.Current);
    }

    public ValueChange<TValue> CalculateDelta(ValueChange<TValue> previous, ValueChange<TValue> current)
    {
        return new ValueChange<TValue>(
            previous.Current,
            current.Current
        );
    }

    public ValueChange<TValue> AddDelta(ValueChange<TValue> value, ValueChange<TValue> delta)
    {
        return new ValueChange<TValue>(
            value.Original,
            delta.Current
        );
    }
}

public record ValueChange<TValue>(
    TValue Original,
    TValue Current
);

namespace LLL.ComputedExpression.ChangeCalculations;

public class ValueChangeCalculation<TValue>(
    bool incremental,
    IEqualityComparer<TValue> comparer
) : IChangeCalculation<TValue, ValueChange<TValue>>
{
    public bool IsIncremental => incremental;
    public bool PreLoadEntities => true;

    public ValueChange<TValue> GetChange(IComputedValues<TValue> computedValues)
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

    public ValueChange<TValue> DeltaChange(ValueChange<TValue> previous, ValueChange<TValue> current)
    {
        return new ValueChange<TValue>(
            previous.Current,
            current.Current
        );
    }

    public ValueChange<TValue> ApplyChange(ValueChange<TValue> value, ValueChange<TValue> change)
    {
        return new ValueChange<TValue>(
            value.Original,
            change.Current
        );
    }
}

public readonly struct ValueChange<TValue>(TValue? original, TValue current)
{
    public TValue? Original => original;
    public TValue Current => current;
}
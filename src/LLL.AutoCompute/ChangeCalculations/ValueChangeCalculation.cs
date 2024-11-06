
namespace LLL.AutoCompute.ChangeCalculations;

public record class ValueChangeCalculation<TValue>(IEqualityComparer<TValue>? comparer = null)
    : IChangeCalculation<TValue, ValueChange<TValue>>
{
    public bool IsIncremental => false;
    public bool PreLoadEntities => true;
    public IEqualityComparer<TValue> Comparer { get; } = comparer ?? EqualityComparer<TValue>.Default;

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
        return Comparer.Equals(result.Original, result.Current);
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

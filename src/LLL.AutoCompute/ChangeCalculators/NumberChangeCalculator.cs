using System.Numerics;

namespace LLL.AutoCompute.ChangeCalculations;

public record class NumberChangeCalculator<TValue>(bool IsIncremental)
    : IChangeCalculator<TValue, TValue>
    where TValue : INumber<TValue>
{
    public ComputedValuesMode ComputedValuesMode => IsIncremental
        ? ComputedValuesMode.Incremental
        : ComputedValuesMode.Full;

    public TValue GetChange(IComputedValues<TValue> computedValues)
    {
        return computedValues.GetCurrentValue() - computedValues.GetOriginalValue();
    }

    public bool IsNoChange(TValue result)
    {
        return TValue.IsZero(result);
    }

    public TValue DeltaChange(TValue previous, TValue current)
    {
        return current - previous;
    }

    public TValue ApplyChange(TValue value, TValue change)
    {
        return value + change;
    }
}
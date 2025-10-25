
namespace LLL.AutoCompute.ChangeCalculations;

public record class CurrentValueChangeCalculator<TValue>(bool IsIncremental)
    : IChangeCalculator<TValue, TValue>
{
    public ComputedValueStrategy ValueStrategy => IsIncremental
        ? ComputedValueStrategy.Incremental
        : ComputedValueStrategy.Full;

    public TValue GetChange(IComputedValues<TValue> computedValues)
    {
        return computedValues.GetCurrentValue();
    }

    public bool IsNoChange(TValue result)
    {
        return false;
    }

    public TValue DeltaChange(TValue previous, TValue current)
    {
        return current;
    }

    public TValue ApplyChange(TValue value, TValue change)
    {
        return change;
    }
}
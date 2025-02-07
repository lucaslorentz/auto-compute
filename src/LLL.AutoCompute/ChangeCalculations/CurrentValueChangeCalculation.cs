
namespace LLL.AutoCompute.ChangeCalculations;

public record class CurrentValueChangeCalculation<TValue>(bool IsIncremental)
    : IChangeCalculation<TValue, TValue>
{
    public bool PreLoadEntities => true;

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
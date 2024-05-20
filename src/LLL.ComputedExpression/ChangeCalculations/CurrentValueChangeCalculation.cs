
namespace LLL.ComputedExpression.ChangeCalculations;

public class CurrentValueChangeCalculation<TValue>
    : IChangeCalculation<TValue, TValue>
{
    public bool IsIncremental => false;
    public bool PreLoadEntities => true;

    public TValue GetChange(IComputedValues<TValue> computedValues)
    {
        return computedValues.GetCurrentValue();
    }

    public bool IsNoChange(TValue result)
    {
        return false;
    }

    public TValue CalculateDelta(TValue previous, TValue current)
    {
        return current;
    }

    public TValue AddDelta(TValue value, TValue delta)
    {
        return delta;
    }
}
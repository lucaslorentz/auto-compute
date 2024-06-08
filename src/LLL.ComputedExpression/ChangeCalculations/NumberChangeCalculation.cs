using System.Numerics;

namespace LLL.ComputedExpression.ChangeCalculations;

public class NumberChangeCalculation<TValue>(bool incremental)
    : IChangeCalculation<TValue, TValue>
    where TValue : INumber<TValue>
{
    public bool IsIncremental => incremental;
    public bool PreLoadEntities => true;

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
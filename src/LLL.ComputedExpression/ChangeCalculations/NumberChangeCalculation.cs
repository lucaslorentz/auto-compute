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

    public TValue CalculateDelta(TValue previous, TValue current)
    {
        return current - previous;
    }

    public TValue AddDelta(TValue value, TValue delta)
    {
        return value + delta;
    }
}
using System.Numerics;

namespace LLL.ComputedExpression.ChangeCalculations;

public class NumberChangeCalculation<TValue> : IncrementalChangeCalculation<TValue, TValue>
    where TValue : INumber<TValue>
{
    protected override TValue CalculateChange(TValue original, TValue current)
    {
        return current - original;
    }

    public override bool IsNoChange(TValue result)
    {
        return TValue.IsZero(result);
    }

    public override TValue CalculateDelta(TValue previous, TValue current)
    {
        return current - previous;
    }
}
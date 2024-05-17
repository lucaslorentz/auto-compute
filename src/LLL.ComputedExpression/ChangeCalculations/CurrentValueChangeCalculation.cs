
namespace LLL.ComputedExpression.ChangeCalculations;

public class CurrentValueChangeCalculation<TValue> : IChangeCalculation<TValue, TValue>
{
    public async Task<TValue> GetChangeAsync(IComputedValues<TValue> computedValues)
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
}
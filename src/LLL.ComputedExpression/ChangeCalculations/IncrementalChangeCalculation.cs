
namespace LLL.ComputedExpression.ChangeCalculations;

public abstract class IncrementalChangeCalculation<TValue, TResult>
    : IChangeCalculation<TValue, TResult>
{
    public async Task<TResult> GetChangeAsync(ComputedValues<TValue> computedValues)
    {
        return CalculateChange(computedValues.GetIncrementalOriginalValue(), computedValues.GetIncrementalCurrentValue());
    }

    protected abstract TResult CalculateChange(TValue original, TValue current);

    public abstract bool IsNoChange(TResult result);

    public abstract TResult CalculateDelta(TResult previous, TResult current);
}

namespace LLL.ComputedExpression.ChangeCalculations;

public abstract class IncrementalChangeCalculation<TValue, TResult>
    : IChangeCalculation<TValue, TResult>
{
    public bool IsIncremental => true;

    public async Task<TResult> GetChangeAsync(IComputedValues<TValue> computedValues)
    {
        return CalculateChange(computedValues.GetOriginalValue(), computedValues.GetCurrentValue());
    }

    protected abstract TResult CalculateChange(TValue original, TValue current);

    public abstract bool IsNoChange(TResult result);

    public abstract TResult CalculateDelta(TResult previous, TResult current);

    public abstract TResult AddDelta(TResult value, TResult delta);
}
namespace LLL.ComputedExpression;

public interface IComputedValues<TValue>
{
    TValue GetOriginalValue();
    TValue GetCurrentValue();
    TValue GetIncrementalOriginalValue();
    TValue GetIncrementalCurrentValue();
}
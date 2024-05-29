namespace LLL.ComputedExpression;

public interface IComputedValues<out TValue>
{
    TValue GetOriginalValue();
    TValue GetCurrentValue();
}

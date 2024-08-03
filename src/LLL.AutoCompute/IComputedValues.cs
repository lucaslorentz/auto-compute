namespace LLL.AutoCompute;

public interface IComputedValues<out TValue>
{
    TValue GetOriginalValue();
    TValue GetCurrentValue();
}

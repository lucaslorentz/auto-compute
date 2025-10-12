namespace LLL.AutoCompute.ChangeCalculations;

public readonly struct ValueChange<TValue>(TValue? original, TValue current)
{
    public TValue? Original => original;
    public TValue Current => current;
}
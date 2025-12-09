namespace LLL.AutoCompute;

public sealed class ChangeCalculatorFactory<TValue> : IChangeCalculatorFactory<TValue>
{
    private ChangeCalculatorFactory() { }

    public static ChangeCalculatorFactory<TValue> Instance { get; } = new ChangeCalculatorFactory<TValue>();
}

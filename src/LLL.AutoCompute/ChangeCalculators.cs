namespace LLL.AutoCompute;

public sealed class ChangeCalculators<TValue> : IChangeCalculators<TValue>
{
    private ChangeCalculators() { }

    public static ChangeCalculators<TValue> Instance { get; } = new ChangeCalculators<TValue>();
}
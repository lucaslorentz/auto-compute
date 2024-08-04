namespace LLL.AutoCompute;

public class ChangeCalculations<TValue> : IChangeCalculations<TValue>
{
    protected ChangeCalculations() { }
    
    public static ChangeCalculations<TValue> Instance { get; } = new ChangeCalculations<TValue>();
}
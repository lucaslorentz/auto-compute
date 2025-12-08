namespace LLL.AutoCompute.ChangesProviders;

public class ComputedValues<TEntity, TValue>(
    ComputedInput input,
    TEntity entity,
    Func<ComputedInput, TEntity, TValue> originalValueGetter,
    Func<ComputedInput, TEntity, TValue> currentValueGetter)
    : IComputedValues<TValue>
{
    public TValue GetOriginalValue() => originalValueGetter(input, entity);
    public TValue GetCurrentValue() => currentValueGetter(input, entity);
}

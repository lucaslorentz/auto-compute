namespace LLL.AutoCompute.ChangesProviders;

public class ComputedValues<TInput, TEntity, TValue>(
    TInput input,
    TEntity entity,
    Func<TInput, TEntity, TValue> originalValueGetter,
    Func<TInput, TEntity, TValue> currentValueGetter)
    : IComputedValues<TValue>
{
    public TValue GetOriginalValue() => originalValueGetter(input, entity);
    public TValue GetCurrentValue() => currentValueGetter(input, entity);
}
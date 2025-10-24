namespace LLL.AutoCompute.ChangesProviders;

public class ComputedValues<TInput, TEntity, TValue>(
    TInput input,
    IncrementalContext? incrementalContext,
    TEntity entity,
    Func<TInput, IncrementalContext?, TEntity, TValue> originalValueGetter,
    Func<TInput, IncrementalContext?, TEntity, TValue> currentValueGetter)
    : IComputedValues<TValue>
{
    public TValue GetOriginalValue() => originalValueGetter(input, incrementalContext, entity);
    public TValue GetCurrentValue() => currentValueGetter(input, incrementalContext, entity);
}
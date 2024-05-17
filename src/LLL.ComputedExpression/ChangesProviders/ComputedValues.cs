namespace LLL.ComputedExpression.ChangesProviders;

public class ComputedValues<TInput, TEntity, TValue>(
    TInput input,
    IncrementalContext incrementalContext,
    TEntity entity,
    ComputedValueAccessors<TInput, TEntity, TValue> computedValues)
    : IComputedValues<TValue>
{
    public TValue GetOriginalValue() => computedValues.GetOriginalValue(input, entity, incrementalContext);
    public TValue GetCurrentValue() => computedValues.GetCurrentValue(input, entity, incrementalContext);
    public TValue GetIncrementalOriginalValue() => computedValues.GetIncrementalOriginalValue(input, entity, incrementalContext);
    public TValue GetIncrementalCurrentValue() => computedValues.GetIncrementalCurrentValue(input, entity, incrementalContext);
}
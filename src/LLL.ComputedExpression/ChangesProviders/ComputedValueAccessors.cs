namespace LLL.ComputedExpression.ChangesProviders;

public class ComputedValueAccessors<TInput, TEntity, TValue>(
    Func<TInput, IncrementalContext, TEntity, TValue> originalValueGetter,
    Func<TInput, IncrementalContext, TEntity, TValue> currentValueGetter)
{
    public TValue GetOriginalValue(TInput input, TEntity entity, IncrementalContext incrementalContext) => originalValueGetter(input, incrementalContext, entity);
    public TValue GetCurrentValue(TInput input, TEntity entity, IncrementalContext incrementalContext) => currentValueGetter(input, incrementalContext, entity);
}
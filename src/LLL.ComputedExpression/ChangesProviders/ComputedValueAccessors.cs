namespace LLL.ComputedExpression.ChangesProviders;

public class ComputedValueAccessors<TInput, TEntity, TValue>(
    Func<TInput, IncrementalContext, TEntity, TValue> originalValueGetter,
    Func<TInput, IncrementalContext, TEntity, TValue> currentValueGetter,
    Func<TInput, IncrementalContext, TEntity, TValue> incrementalOriginalValueGetter,
    Func<TInput, IncrementalContext, TEntity, TValue> incrementalCurrentValueGetter)
{
    public TValue GetOriginalValue(TInput input, TEntity entity, IncrementalContext incrementalContext) => originalValueGetter(input, incrementalContext, entity);
    public TValue GetCurrentValue(TInput input, TEntity entity, IncrementalContext incrementalContext) => currentValueGetter(input, incrementalContext, entity);
    public TValue GetIncrementalOriginalValue(TInput input, TEntity entity, IncrementalContext incrementalContext) => incrementalOriginalValueGetter(input, incrementalContext, entity);
    public TValue GetIncrementalCurrentValue(TInput input, TEntity entity, IncrementalContext incrementalContext) => incrementalCurrentValueGetter(input, incrementalContext, entity);
}
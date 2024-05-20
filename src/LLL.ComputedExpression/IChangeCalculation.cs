namespace LLL.ComputedExpression;

public interface IChangeCalculation<TResult>
{
    bool IsIncremental { get; }
    bool PreLoadEntities { get; }
    bool IsNoChange(TResult result);
    TResult CalculateDelta(TResult previous, TResult current);
    TResult AddDelta(TResult original, TResult delta);
}

public interface IChangeCalculation<TValue, TResult> : IChangeCalculation<TResult>
{
    TResult GetChange(IComputedValues<TValue> computedValues);
}
namespace LLL.ComputedExpression;

public interface IChangeCalculation
{
    Type ResultType { get; }
}

public interface IChangeCalculation<TResult> : IChangeCalculation
{
    Type IChangeCalculation.ResultType => typeof(TResult);
    bool IsNoChange(TResult result);
    TResult CalculateDelta(TResult previous, TResult current);
    TResult AddDelta(TResult original, TResult delta);
}

public interface IChangeCalculation<TValue, TResult> : IChangeCalculation<TResult>
{
    Task<TResult> GetChangeAsync(IComputedValues<TValue> affectedEntity);
}
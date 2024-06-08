namespace LLL.ComputedExpression;

public interface IChangeCalculation<TChange>
{
    bool IsIncremental { get; }
    bool PreLoadEntities { get; }
    bool IsNoChange(TChange result);
    TChange DeltaChange(TChange previous, TChange current);
    TChange ApplyChange(TChange original, TChange change);
}

public interface IChangeCalculation<in TValue, TChange> : IChangeCalculation<TChange>
{
    TChange GetChange(IComputedValues<TValue> computedValues);
}
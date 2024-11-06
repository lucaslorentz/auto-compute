namespace LLL.AutoCompute;

public interface IChangeCalculation
{
    bool IsIncremental { get; }
    bool PreLoadEntities { get; }
}

public interface IChangeCalculation<TChange> : IChangeCalculation
{
    bool IsNoChange(TChange result);
    TChange DeltaChange(TChange previous, TChange current);
    TChange ApplyChange(TChange original, TChange change);
}

public interface IChangeCalculation<in TValue, TChange> : IChangeCalculation<TChange>
{
    TChange GetChange(IComputedValues<TValue> computedValues);
}
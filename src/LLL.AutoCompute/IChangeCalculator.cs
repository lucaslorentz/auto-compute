namespace LLL.AutoCompute;

public interface IChangeCalculator
{
    ComputedValueStrategy ValueStrategy { get; }
}

public interface IChangeCalculator<TChange> : IChangeCalculator
{
    bool IsNoChange(TChange result);
    TChange DeltaChange(TChange previous, TChange current);
    TChange ApplyChange(TChange original, TChange change);
}

public interface IChangeCalculator<in TValue, TChange> : IChangeCalculator<TChange>
{
    TChange GetChange(IComputedValues<TValue> computedValues);
}

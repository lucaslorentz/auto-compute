namespace LLL.AutoCompute;

/// <summary>
/// Non-generic base interface for change calculators.
/// </summary>
public interface IChangeCalculator
{
    /// <summary>The strategy used for computing values (Full or Incremental).</summary>
    ComputedValueStrategy ValueStrategy { get; }
}

/// <summary>
/// Provides operations for working with computed changes.
/// </summary>
/// <typeparam name="TChange">The change representation type.</typeparam>
public interface IChangeCalculator<TChange> : IChangeCalculator
{
    /// <summary>Determines if the change represents no actual change.</summary>
    bool IsNoChange(TChange change);

    /// <summary>Calculates the delta between a previous and current change.</summary>
    TChange DeltaChange(TChange previous, TChange current);

    /// <summary>Applies a change to an original value to produce the result.</summary>
    TChange ApplyChange(TChange original, TChange change);
}

/// <summary>
/// Calculates changes by comparing original and current computed values.
/// </summary>
/// <typeparam name="TValue">The computed value type.</typeparam>
/// <typeparam name="TChange">The change representation type.</typeparam>
public interface IChangeCalculator<in TValue, TChange> : IChangeCalculator<TChange>
{
    /// <summary>Computes the change between original and current values.</summary>
    TChange GetChange(IComputedValues<TValue> computedValues);
}

namespace LLL.AutoCompute;

/// <summary>
/// Provides access to original and current computed values for change detection.
/// </summary>
/// <typeparam name="TValue">The computed value type.</typeparam>
public interface IComputedValues<out TValue>
{
    /// <summary>Gets the original value (before any changes were made).</summary>
    TValue GetOriginalValue();

    /// <summary>Gets the current value (after changes).</summary>
    TValue GetCurrentValue();
}

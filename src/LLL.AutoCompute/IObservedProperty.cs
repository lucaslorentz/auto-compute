namespace LLL.AutoCompute;

/// <summary>
/// Represents an observed scalar property on an entity type.
/// </summary>
public interface IObservedProperty : IObservedMember
{
    /// <summary>The entity type that owns this property.</summary>
    IObservedEntityType EntityType { get; }

    /// <summary>Gets all property changes detected in the current input context.</summary>
    Task<IReadOnlyList<ObservedPropertyChange>> GetChangesAsync(ComputedInput input);
}

namespace LLL.AutoCompute;

/// <summary>
/// Represents an observed navigation property (reference or collection) between entity types.
/// </summary>
public interface IObservedNavigation : IObservedMember
{
    /// <summary>The entity type that this navigation originates from.</summary>
    IObservedEntityType SourceEntityType { get; }

    /// <summary>The entity type that this navigation points to.</summary>
    IObservedEntityType TargetEntityType { get; }

    /// <summary>True if this is a collection navigation; false for reference navigations.</summary>
    bool IsCollection { get; }

    /// <summary>The change propagation target configured for this navigation.</summary>
    ChangePropagationTarget? ChangePropagationTarget { get; }

    /// <summary>Gets the inverse navigation (from target back to source), if available and non-shadow.</summary>
    IObservedNavigation? GetInverse();

    /// <summary>Gets the inverse navigation and throws when it is not available.</summary>
    IObservedNavigation GetInverseOrThrow();

    /// <summary>Loads the current related entities for the given source entities.</summary>
    Task<IReadOnlyDictionary<object, IReadOnlyCollection<object>>> LoadCurrentAsync(ComputedInput input, IReadOnlyCollection<object> fromEntities);

    /// <summary>Loads the original related entities for the given source entities.</summary>
    Task<IReadOnlyDictionary<object, IReadOnlyCollection<object>>> LoadOriginalAsync(ComputedInput input, IReadOnlyCollection<object> fromEntities);

    /// <summary>Gets all navigation changes detected in the current input context.</summary>
    Task<IReadOnlyList<ObservedNavigationChange>> GetChangesAsync(ComputedInput input);
}

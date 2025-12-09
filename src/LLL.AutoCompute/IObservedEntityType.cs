namespace LLL.AutoCompute;

/// <summary>
/// Represents metadata for an observed entity type in the change tracking system.
/// </summary>
public interface IObservedEntityType
{
    /// <summary>The name of the entity type.</summary>
    string Name { get; }

    /// <summary>Gets the current state of an entity.</summary>
    ObservedEntityState GetEntityState(ComputedInput input, object entity);

    /// <summary>Checks if an object is an instance of this entity type.</summary>
    bool IsInstanceOfType(object obj);
}

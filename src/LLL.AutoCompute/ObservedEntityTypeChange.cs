namespace LLL.AutoCompute;

/// <summary>
/// Represents changes that happened to an entity type.
/// </summary>
public class ObservedEntityTypeChange
{
    private readonly HashSet<object> _added = [];
    private readonly HashSet<object> _removed = [];

    /// <summary>Indicates whether any changes have been registered.</summary>
    public bool IsEmpty => _added.Count == 0 && _removed.Count == 0;

    /// <summary>Entities that were added.</summary>
    public IReadOnlySet<object> Added => _added;

    /// <summary>Entities that were removed.</summary>
    public IReadOnlySet<object> Removed => _removed;

    /// <summary>
    /// Registers that an entity was added.
    /// </summary>
    /// <param name="relatedEntity"></param>
    public void RegisterAdded(object relatedEntity)
    {
        if (_removed.Contains(relatedEntity))
            _removed.Remove(relatedEntity);
        else
            _added.Add(relatedEntity);
    }

    /// <summary>
    /// Registers that an entity was removed.
    /// </summary>
    /// <param name="relatedEntity"></param>
    public void RegisterRemoved(object relatedEntity)
    {
        if (_added.Contains(relatedEntity))
            _added.Remove(relatedEntity);
        else
            _removed.Add(relatedEntity);
    }
}
namespace LLL.AutoCompute;

/// <summary>
/// Represents changes that happened to a navigation property of an entity.
/// </summary>
public class ObservedNavigationChange(object entity)
{
    private readonly HashSet<object> _added = [];
    private readonly HashSet<object> _removed = [];

    /// <summary>The entity that had this navigation change.</summary>
    public object Entity => entity;

    /// <summary>Indicates whether any changes have been registered.</summary>
    public bool IsEmpty => _added.Count == 0 && _removed.Count == 0;

    /// <summary>Entities that were not in the original value and are now present</summary>
    public IReadOnlySet<object> Added => _added;

    /// <summary>Entities that were in the original value and are now absent</summary>
    public IReadOnlySet<object> Removed => _removed;

    public void RegisterAdded(object relatedEntity)
    {
        if (_removed.Contains(relatedEntity))
            _removed.Remove(relatedEntity);
        else
            _added.Add(relatedEntity);
    }

    public void RegisterRemoved(object relatedEntity)
    {
        if (_added.Contains(relatedEntity))
            _added.Remove(relatedEntity);
        else
            _removed.Add(relatedEntity);
    }
}
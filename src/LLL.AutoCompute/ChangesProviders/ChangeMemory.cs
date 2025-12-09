using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace LLL.AutoCompute.ChangesProviders;

/// <summary>
/// Stores previously computed changes per entity for delta calculations.
/// Uses weak references so entries are automatically removed when entities are garbage collected.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TChange">The change type.</typeparam>
public class ChangeMemory<TEntity, TChange>
    where TEntity : class
{
    private readonly ConditionalWeakTable<TEntity, ValueWrapper> _memory = [];

    /// <summary>
    /// Attempts to retrieve the stored change for an entity.
    /// </summary>
    public bool TryGet(TEntity entity, [NotNullWhen(true)] out TChange? result)
    {
        _memory.TryGetValue(entity, out var valueWrapper);

        if (valueWrapper is null)
        {
            result = default;
            return false;
        }

        result = valueWrapper.Value!;
        return true;
    }

    /// <summary>
    /// Stores or updates the change for an entity.
    /// </summary>
    public void AddOrUpdate(TEntity entity, TChange result)
    {
        _memory.AddOrUpdate(entity, new ValueWrapper(result));
    }

    /// <summary>
    /// Removes the stored change for an entity.
    /// </summary>
    public void Remove(TEntity entity)
    {
        _memory.Remove(entity);
    }

    /// <summary>
    /// Gets all entities currently stored in memory.
    /// </summary>
    public IReadOnlyCollection<TEntity> GetEntities()
    {
        return _memory.Select(x => x.Key).ToArray();
    }

    record class ValueWrapper(TChange Value);
}

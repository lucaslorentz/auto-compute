using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace LLL.ComputedExpression.ChangesProviders;

public class ChangeMemory<TEntity, TChange>
    where TEntity : class
{
    private readonly ConditionalWeakTable<TEntity, ValueWrapper> _memory = [];

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

    public void AddOrUpdate(TEntity entity, TChange result)
    {
        _memory.AddOrUpdate(entity, new ValueWrapper(result));
    }

    public void Remove(TEntity entity)
    {
        _memory.Remove(entity);
    }

    public IReadOnlyCollection<TEntity> GetEntities()
    {
        return _memory.Select(x => x.Key).ToArray();
    }

    record class ValueWrapper(TChange Value);
}
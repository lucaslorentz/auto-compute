using System.Collections.Concurrent;

namespace LLL.ComputedExpression;

public class IncrementalContext
{
    private readonly ConcurrentDictionary<(object, object), HashSet<object>> _entities = [];
    private readonly HashSet<object> _shouldLoadAll = [];

    public void AddIncrementalEntity(object entity, object navigation, object loadedEntity)
    {
        var collection = _entities.GetOrAdd((entity, navigation), _ => []);

        collection.Add(loadedEntity);
    }

    public IReadOnlyCollection<object> GetIncrementalEntities(object entity, object navigation)
    {
        if (!_entities.TryGetValue((entity, navigation), out var collection))
            return [];

        return collection;
    }

    public bool ShouldLoadAll(object entity)
    {
        return _shouldLoadAll.Contains(entity);
    }

    public void SetShouldLoadAll(object entity)
    {
        _shouldLoadAll.Add(entity);
    }
}

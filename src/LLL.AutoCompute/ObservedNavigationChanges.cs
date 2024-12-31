using System.Collections.Concurrent;

namespace LLL.AutoCompute;

public class ObservedNavigationChanges
{
    private readonly ConcurrentDictionary<object, EntityChanges> _entityChanges = [];

    public IReadOnlyDictionary<object, EntityChanges> GetEntityChanges() => _entityChanges;

    public bool HasEntityChange(object entity) => _entityChanges.ContainsKey(entity);

    public bool RegisterAdded(object entity, object relatedEntity)
    {
        return GetOrCrateEntityChanges(entity).RegisterAdded(relatedEntity);
    }

    public bool RegisterRemoved(object entity, object relatedEntity)
    {
        return GetOrCrateEntityChanges(entity).RegisterRemoved(relatedEntity);
    }

    private EntityChanges GetOrCrateEntityChanges(object entity)
    {
        return _entityChanges.GetOrAdd(entity, static e => new EntityChanges(e));
    }

    public class EntityChanges(object entity)
    {
        private readonly HashSet<object> _added = [];
        private readonly HashSet<object> _removed = [];

        public object Entity => entity;

        public IReadOnlyCollection<object> Added => _added;
        public IReadOnlyCollection<object> Removed => _removed;

        public bool RegisterAdded(object relatedEntity)
        {
            return _added.Add(relatedEntity);
        }

        public bool RegisterRemoved(object relatedEntity)
        {
            return _removed.Add(relatedEntity);
        }
    }
}
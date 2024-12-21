using System.Collections.Concurrent;

namespace LLL.AutoCompute;

public class ObservedNavigationChanges
{
    private readonly ConcurrentDictionary<object, EntityChanges> _entityChanges = [];

    public IReadOnlyDictionary<object, EntityChanges> GetEntityChanges() => _entityChanges;

    public void RegisterAdded(object entity, object relatedEntity)
    {
        GetOrCrateEntityChanges(entity).RegisterAdded(relatedEntity);
    }

    public void RegisterRemoved(object entity, object relatedEntity)
    {
        GetOrCrateEntityChanges(entity).RegisterRemoved(relatedEntity);
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

        public void RegisterAdded(object relatedEntity)
        {
            _added.Add(relatedEntity);
        }

        public void RegisterRemoved(object relatedEntity)
        {
            _removed.Add(relatedEntity);
        }
    }
}
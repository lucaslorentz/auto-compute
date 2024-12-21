using System.Collections.Concurrent;

namespace LLL.AutoCompute;

public class IncrementalContext
{
    private readonly ConcurrentDictionary<(object, IObservedNavigation), HashSet<object>> _originalEntities = [];
    private readonly ConcurrentDictionary<(object, IObservedNavigation), HashSet<object>> _currentEntities = [];
    private readonly HashSet<object> _shouldLoadAll = [];

    public void AddOriginalEntity(
        object entity,
        IObservedNavigation navigation,
        object loadedEntity)
    {
        var collection = _originalEntities.GetOrAdd((entity, navigation), _ => []);

        collection.Add(loadedEntity);
    }

    public void AddCurrentEntity(
        object entity,
        IObservedNavigation navigation,
        object loadedEntity)
    {
        var collection = _currentEntities.GetOrAdd((entity, navigation), _ => []);

        collection.Add(loadedEntity);
    }

    public IReadOnlyCollection<object> GetOriginalEntities(object entity, IObservedNavigation navigation)
    {
        if (!_originalEntities.TryGetValue((entity, navigation), out var collection))
            return [];

        return collection;
    }

    public IReadOnlyCollection<object> GetCurrentEntities(object entity, IObservedNavigation navigation)
    {
        if (!_currentEntities.TryGetValue((entity, navigation), out var collection))
            return [];

        return collection;
    }

    public IReadOnlyCollection<object> GetEntities(object entity, IObservedNavigation navigation)
    {
        return GetOriginalEntities(entity, navigation)
            .Concat(GetCurrentEntities(entity, navigation))
            .ToHashSet();
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

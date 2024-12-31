namespace LLL.AutoCompute;

public class ObservedPropertyChanges
{
    private readonly HashSet<object> _entityChanges = [];

    public IReadOnlySet<object> GetEntityChanges() => _entityChanges;

    public bool RegisterChange(object entity)
    {
        return _entityChanges.Add(entity);
    }
}
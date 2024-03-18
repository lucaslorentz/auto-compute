namespace LLL.Computed;

public interface IEntityContext
{
    bool IsTrackingChanges { get; }
    void AddAffectedEntitiesProvider(IAffectedEntitiesProvider provider);
}

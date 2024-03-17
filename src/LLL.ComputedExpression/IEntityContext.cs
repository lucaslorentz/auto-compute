namespace L3.Computed;

public interface IEntityContext
{
    bool IsTrackingChanges { get; }
    void AddAffectedEntitiesProvider(IAffectedEntitiesProvider provider);
}

namespace LLL.AutoCompute.AffectedEntitiesProviders;

public class NavigationAffectedEntitiesProvider<TInput, TSourceEntity, TTargetEntity>(IEntityNavigation<TInput, TSourceEntity, TTargetEntity> navigation)
      : IAffectedEntitiesProvider<TInput, TSourceEntity>
      where TSourceEntity : class
      where TTargetEntity : class
{
    public virtual string ToDebugString()
    {
        return $"EntitiesWithNavigationChange({navigation.ToDebugString()})";
    }

    public virtual async Task<IReadOnlyCollection<TSourceEntity>> GetAffectedEntitiesAsync(TInput input, IncrementalContext incrementalContext)
    {
        return await navigation.GetAffectedEntitiesAsync(input, incrementalContext);
    }
}
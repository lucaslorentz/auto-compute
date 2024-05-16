namespace LLL.ComputedExpression.AffectedEntitiesProviders;

public class LoadNavigationAffectedEntitiesProvider<TInput, TSourceEntity, TTargetEntity>(
    IAffectedEntitiesProvider<TInput, TSourceEntity> affectedEntitiesProvider,
    IEntityNavigation<TInput, TSourceEntity, TTargetEntity> navigation
) : IAffectedEntitiesProvider<TInput, TTargetEntity>
{
    public string ToDebugString()
    {
        return $"Load({affectedEntitiesProvider.ToDebugString()}, {navigation.Name})";
    }

    public async Task<IReadOnlyCollection<TTargetEntity>> GetAffectedEntitiesAsync(TInput input, IncrementalContext? incrementalContext)
    {
        var affectedEntities = await affectedEntitiesProvider.GetAffectedEntitiesAsync(input, incrementalContext);
        var entities = new HashSet<TTargetEntity>();
        foreach (var ent in await navigation.LoadOriginalAsync(input, affectedEntities, incrementalContext))
            entities.Add(ent);
        foreach (var ent in await navigation.LoadCurrentAsync(input, affectedEntities, incrementalContext))
            entities.Add(ent);
        return entities;
    }
}

namespace LLL.ComputedExpression.AffectedEntitiesProviders;

public class PropertyAffectedEntitiesProvider<TInput, TEntity>(IEntityProperty<TInput, TEntity> property)
      : IAffectedEntitiesProvider<TInput, TEntity>
      where TEntity : class
{
    public virtual string ToDebugString()
    {
        return $"EntitiesWithPropertyChange({property.ToDebugString()})";
    }

    public virtual async Task<IReadOnlyCollection<TEntity>> GetAffectedEntitiesAsync(TInput input, IncrementalContext incrementalContext)
    {
        return await property.GetAffectedEntitiesAsync(input, incrementalContext);
    }
}
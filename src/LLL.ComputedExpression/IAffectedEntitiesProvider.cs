namespace LLL.ComputedExpression;

public interface IAffectedEntitiesProvider
{
    Type InputType { get; }
    Type EntityType { get; }

    string ToDebugString();

    IReadOnlyCollection<IAffectedEntitiesProvider> Flatten()
    {
        return [this];
    }
}

public interface IAffectedEntitiesProvider<in TInput, TEntity> : IAffectedEntitiesProvider
{
    Type IAffectedEntitiesProvider.InputType => typeof(TInput);
    Type IAffectedEntitiesProvider.EntityType => typeof(TEntity);

    Task<IReadOnlyCollection<TEntity>> GetAffectedEntitiesAsync(TInput input, IncrementalContext? incrementalContext = null);

    new IReadOnlyCollection<IAffectedEntitiesProvider<TInput, TEntity>> Flatten()
    {
        return [this];
    }

    IReadOnlyCollection<IAffectedEntitiesProvider> IAffectedEntitiesProvider.Flatten()
    {
        return Flatten();
    }
}

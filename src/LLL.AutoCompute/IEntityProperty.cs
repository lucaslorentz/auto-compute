
namespace LLL.AutoCompute;

public interface IEntityProperty : IEntityMember<IEntityProperty>
{
    Type EntityType { get; }
}

public interface IEntityProperty<in TInput, TEntity> : IEntityProperty
{
    Type IEntityMember.InputType => typeof(TInput);
    Type IEntityProperty.EntityType => typeof(TEntity);

    Task<IReadOnlyCollection<TEntity>> GetAffectedEntitiesAsync(TInput input, IncrementalContext incrementalContext);

    async Task<IReadOnlyCollection<object>> IEntityMember.GetAffectedEntitiesAsync(object input, IncrementalContext incrementalContext)
    {
        if (input is not TInput inputTyped)
            throw new ArgumentException($"Param {nameof(input)} should be of type {typeof(TInput)}");

        return (IReadOnlyCollection<object>)await GetAffectedEntitiesAsync(inputTyped, incrementalContext);
    }
}
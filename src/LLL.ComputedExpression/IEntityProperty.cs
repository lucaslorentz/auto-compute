
namespace LLL.ComputedExpression;

public interface IEntityProperty : IEntityMember<IEntityProperty>
{
    Type EntityType { get; }
}

public interface IEntityProperty<in TInput, TEntity> : IEntityProperty
{
    Type IEntityMember.InputType => typeof(TInput);
    Type IEntityProperty.EntityType => typeof(TEntity);

    Task<IReadOnlyCollection<TEntity>> GetAffectedEntitiesAsync(TInput input, IncrementalContext? incrementalContext);
}
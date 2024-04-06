namespace LLL.ComputedExpression;

public interface IEntityProperty : IEntityMember<IEntityProperty>
{
}

public interface IEntityProperty<in TInput, TEntity> : IEntityProperty
{
}
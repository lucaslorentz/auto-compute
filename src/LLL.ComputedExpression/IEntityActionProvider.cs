namespace LLL.ComputedExpression;

public interface IEntityActionProvider
{
    EntityAction GetEntityAction(object input, object entity);
}

public interface IEntityActionProvider<TInput> : IEntityActionProvider
{
    EntityAction GetEntityAction(TInput input, object entity);

    EntityAction IEntityActionProvider.GetEntityAction(object input, object entity)
    {
        return GetEntityAction((TInput)input, entity);
    }
}
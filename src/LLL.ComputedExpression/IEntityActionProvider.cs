namespace LLL.ComputedExpression;

public interface IEntityActionProvider
{
    EntityAction GetEntityAction(object input, object entity);
}

public interface IEntityActionProvider<in TInput> : IEntityActionProvider
{
    EntityAction GetEntityAction(TInput input, object entity);

    EntityAction IEntityActionProvider.GetEntityAction(object input, object entity)
    {
        if (input is not TInput inputTyped)
            throw new ArgumentException($"Param {nameof(input)} should be of type {typeof(TInput)}");

        return GetEntityAction(inputTyped, entity);
    }
}
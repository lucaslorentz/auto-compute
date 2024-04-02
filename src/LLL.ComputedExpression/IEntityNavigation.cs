namespace LLL.ComputedExpression;

public interface IEntityNavigation : IEntityMember<IEntityNavigation>
{
    bool IsCollection { get; }
    IEntityNavigation GetInverse();
    Task<IReadOnlyCollection<object>> LoadCurrentAsync(object input, IReadOnlyCollection<object> fromEntities);
    Task<IReadOnlyCollection<object>> LoadOriginalAsync(object input, IReadOnlyCollection<object> fromEntities);
}

public interface IEntityNavigation<in TInput> : IEntityNavigation
{
    new IEntityNavigation<TInput> GetInverse();

    IEntityNavigation IEntityNavigation.GetInverse()
    {
        return GetInverse();
    }

    Task<IReadOnlyCollection<object>> LoadCurrentAsync(TInput input, IReadOnlyCollection<object> fromEntities);

    Task<IReadOnlyCollection<object>> LoadOriginalAsync(TInput input, IReadOnlyCollection<object> fromEntities);

    Task<IReadOnlyCollection<object>> IEntityNavigation.LoadCurrentAsync(object input, IReadOnlyCollection<object> fromEntities)
    {
        if (input is not TInput inputTyped)
            throw new ArgumentException($"Param {nameof(input)} should be of type {typeof(TInput)}");

        return LoadCurrentAsync(inputTyped, fromEntities);
    }

    Task<IReadOnlyCollection<object>> IEntityNavigation.LoadOriginalAsync(object input, IReadOnlyCollection<object> fromEntities)
    {
        if (input is not TInput inputTyped)
            throw new ArgumentException($"Param {nameof(input)} should be of type {typeof(TInput)}");

        return LoadOriginalAsync(inputTyped, fromEntities);
    }
}

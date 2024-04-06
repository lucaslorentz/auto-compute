namespace LLL.ComputedExpression;

public interface IEntityNavigation : IEntityMember<IEntityNavigation>
{
    Type TargetEntityType { get; }
    bool IsCollection { get; }
    IEntityNavigation GetInverse();
    Task<IReadOnlyCollection<object>> LoadCurrentAsync(object input, IReadOnlyCollection<object> fromEntities);
    Task<IReadOnlyCollection<object>> LoadOriginalAsync(object input, IReadOnlyCollection<object> fromEntities);
}

public interface IEntityNavigation<in TInput, TSourceEntity, TTargetEntity> : IEntityNavigation
{
    new IEntityNavigation<TInput, TTargetEntity, TSourceEntity> GetInverse();

    IEntityNavigation IEntityNavigation.GetInverse()
    {
        return GetInverse();
    }

    Task<IReadOnlyCollection<TTargetEntity>> LoadCurrentAsync(TInput input, IReadOnlyCollection<TSourceEntity> fromEntities);

    Task<IReadOnlyCollection<TTargetEntity>> LoadOriginalAsync(TInput input, IReadOnlyCollection<TSourceEntity> fromEntities);

    async Task<IReadOnlyCollection<object>> IEntityNavigation.LoadCurrentAsync(object input, IReadOnlyCollection<object> fromEntities)
    {
        if (input is not TInput inputTyped)
            throw new ArgumentException($"Param {nameof(input)} should be of type {typeof(TInput)}");

        return (IReadOnlyCollection<object>)await LoadCurrentAsync(inputTyped, fromEntities.OfType<TSourceEntity>().ToArray());
    }

    async Task<IReadOnlyCollection<object>> IEntityNavigation.LoadOriginalAsync(object input, IReadOnlyCollection<object> fromEntities)
    {
        if (input is not TInput inputTyped)
            throw new ArgumentException($"Param {nameof(input)} should be of type {typeof(TInput)}");

        return (IReadOnlyCollection<object>)await LoadOriginalAsync(inputTyped, fromEntities.OfType<TSourceEntity>().ToArray());
    }
}

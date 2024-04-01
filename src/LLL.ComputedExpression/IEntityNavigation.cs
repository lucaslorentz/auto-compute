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
        return LoadCurrentAsync((TInput)input, fromEntities);
    }

    Task<IReadOnlyCollection<object>> IEntityNavigation.LoadOriginalAsync(object input, IReadOnlyCollection<object> fromEntities)
    {
        return LoadOriginalAsync((TInput)input, fromEntities);
    }
}

namespace LLL.Computed;

public interface IEntityNavigation : IEntityMember<IEntityNavigation>
{
    bool IsCollection { get; }
    Type TargetType { get; }
    IEntityNavigation GetInverse();
    Task<IEnumerable<object>> LoadAsync(object input, IEnumerable<object> fromEntities);
}

public interface IEntityNavigation<in TInput> : IEntityNavigation
{
    new IEntityNavigation<TInput> GetInverse();

    IEntityNavigation IEntityNavigation.GetInverse()
    {
        return GetInverse();
    }

    Task<IEnumerable<object>> LoadAsync(TInput input, IEnumerable<object> fromEntities);

    Task<IEnumerable<object>> IEntityNavigation.LoadAsync(object input, IEnumerable<object> fromEntities)
    {
        return LoadAsync((TInput)input, fromEntities);
    }
}

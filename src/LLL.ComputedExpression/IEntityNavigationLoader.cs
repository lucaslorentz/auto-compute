namespace LLL.Computed;

public interface IEntityNavigationLoader
{
    Task<IEnumerable<object>> LoadAsync(object input, IEnumerable<object> fromEntities);

    string ToDebugString();
}

public interface IEntityNavigationLoader<in TInput> : IEntityNavigationLoader
{
    Task<IEnumerable<object>> LoadAsync(TInput input, IEnumerable<object> fromEntities);

    Task<IEnumerable<object>> IEntityNavigationLoader.LoadAsync(object input, IEnumerable<object> fromEntities)
    {
        return LoadAsync((TInput)input, fromEntities);
    }
}

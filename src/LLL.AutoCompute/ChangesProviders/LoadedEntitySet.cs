namespace LLL.AutoCompute.ChangesProviders;

public sealed class LoadedEntitySet(IReadOnlySet<object> entities)
{
    public IReadOnlySet<object> Entities => entities;
}

using LLL.AutoCompute.EFCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Internal;

public class EFCoreObservedEntityTypeResolver(IModel model)
    : IObservedEntityTypeResolver
{
    public IObservedEntityType? Resolve(Type type)
    {
        return model.FindEntityType(type)?.GetOrCreateObservedEntityType();
    }
}

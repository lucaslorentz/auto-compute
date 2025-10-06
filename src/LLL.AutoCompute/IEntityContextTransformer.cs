using LLL.AutoCompute.EntityContexts;

namespace LLL.AutoCompute;

public interface IEntityContextTransformer
{
    EntityContext Transform(EntityContext context);
}

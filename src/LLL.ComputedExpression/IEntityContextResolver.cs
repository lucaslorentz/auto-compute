using System.Linq.Expressions;

namespace LLL.Computed;

public interface IEntityContextResolver
{
    IEntityContext? ResolveEntityContext(
        Expression node,
        IComputedExpressionAnalysis analysis,
        string key);
}

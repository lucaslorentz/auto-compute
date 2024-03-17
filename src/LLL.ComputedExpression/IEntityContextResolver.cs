using System.Linq.Expressions;

namespace L3.Computed;

public interface IEntityContextResolver
{
    IEntityContext? ResolveEntityContext(
        Expression node,
        IComputedExpressionAnalysis analysis,
        string key);
}

using System.Linq.Expressions;

namespace LLL.AutoCompute;

public interface IEntityContextNodeRule
{
    void Apply(
        Expression node,
        IEntityContextRegistry entityContextRegistry);
}

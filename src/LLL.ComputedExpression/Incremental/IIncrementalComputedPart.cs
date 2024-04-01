using System.Linq.Expressions;

namespace LLL.ComputedExpression.Incremental;

public interface IIncrementalComputedPart
{
    LambdaExpression Navigation { get; }
    LambdaExpression ValueSelector { get; }
    bool IsMany { get; }
}

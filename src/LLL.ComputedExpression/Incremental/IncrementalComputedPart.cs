using System.Linq.Expressions;

namespace LLL.ComputedExpression.Incremental;

public record IncrementalComputedPart(
    LambdaExpression Navigation,
    LambdaExpression ValueSelector,
    bool IsMany
) : IIncrementalComputedPart;
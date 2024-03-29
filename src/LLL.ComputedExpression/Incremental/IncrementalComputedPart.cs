using System.Linq.Expressions;

namespace LLL.Computed.Incremental;

public record IncrementalComputedPart(
    LambdaExpression Navigation,
    LambdaExpression ValueExtraction,
    Delegate Update,
    bool IsMany
);
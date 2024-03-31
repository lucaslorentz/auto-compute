namespace LLL.ComputedExpression;

public record struct ComputedExpressionAnalysisCacheKey(
    string Analysis,
    ExpressionCacheKey ExpressionKey,
    IComputedExpressionAnalyzer Analyzer
);
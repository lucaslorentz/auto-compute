namespace LLL.Computed;

public record struct ComputedExpressionAnalysisCacheKey(
    string Analysis,
    ExpressionCacheKey ExpressionKey,
    IComputedExpressionAnalyzer Analyzer
);
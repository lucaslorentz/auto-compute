using System.Linq.Expressions;

namespace L3.Computed;

public readonly struct AffectedEntitiesProviderCacheKey(
    LambdaExpression computedExpression,
    IComputedExpressionAnalyzer analyzer,
    IEqualityComparer<Expression> expressionEqualityComparer
) : IEquatable<AffectedEntitiesProviderCacheKey>
{
    private readonly Expression _query = computedExpression;
    private readonly IComputedExpressionAnalyzer _analyzer = analyzer;

    public override bool Equals(object? obj)
        => obj is AffectedEntitiesProviderCacheKey other && Equals(other);

    public bool Equals(AffectedEntitiesProviderCacheKey other)
        => ReferenceEquals(_analyzer, other._analyzer)
            && expressionEqualityComparer.Equals(_query, other._query);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(_query, expressionEqualityComparer);
        hash.Add(_analyzer);
        return hash.ToHashCode();
    }
}

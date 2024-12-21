using System.Linq.Expressions;

namespace LLL.AutoCompute.EFCore.Caching;

public readonly struct ExpressionCacheKey(
    Expression expression,
    IEqualityComparer<Expression> expressionEqualityComparer
) : IEquatable<ExpressionCacheKey>
{
    public Expression Expression => expression;

    public override bool Equals(object? obj)
        => obj is ExpressionCacheKey other && Equals(other);

    public bool Equals(ExpressionCacheKey other)
        => expressionEqualityComparer.Equals(Expression, other.Expression);

    public override int GetHashCode()
    {
        return expressionEqualityComparer.GetHashCode(Expression);
    }

    public static bool operator ==(ExpressionCacheKey left, ExpressionCacheKey right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ExpressionCacheKey left, ExpressionCacheKey right)
    {
        return !(left == right);
    }
}

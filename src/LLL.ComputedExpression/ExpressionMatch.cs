using System.Linq.Expressions;

namespace LLL.Computed;

public class ExpressionMatch<TValue>(
    Expression fromExpression,
    TValue value
) : IExpressionMatch<TValue>
{
    public Expression FromExpression { get; } = fromExpression;
    public TValue Value { get; } = value;
}

public static class ExpressionMatch
{
    public static ExpressionMatch<TValue> Create<TValue>(Expression expression, TValue value)
    {
        return new ExpressionMatch<TValue>(expression, value);
    }
}
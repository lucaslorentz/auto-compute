using System.Linq.Expressions;

namespace LLL.AutoCompute;

public class ObservedMemberAccess(
    Expression expression,
    Expression fromExpression,
    IObservedMember value
)
{
    public Expression Expression { get; } = expression;
    public Expression FromExpression { get; } = fromExpression;
    public IObservedMember Member { get; } = value;
}
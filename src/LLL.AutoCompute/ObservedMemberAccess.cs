using System.Linq.Expressions;

namespace LLL.AutoCompute;

public sealed class ObservedMemberAccess(
    Expression expression,
    Expression fromExpression,
    IObservedMember member
)
{
    public Expression Expression { get; } = expression;
    public Expression FromExpression { get; } = fromExpression;
    public IObservedMember Member { get; } = member;
}


using System.Linq.Expressions;

namespace LLL.AutoCompute;

public abstract class ObservedMemberAccess(
    Expression expression,
    Expression fromExpression,
    IObservedMember value
) : IObservedMemberAccess
{
    public Expression Expression { get; } = expression;
    public Expression FromExpression { get; } = fromExpression;
    public IObservedMember Member { get; } = value;

    public Expression CreateOriginalValueExpression(Expression inputExpression)
    {
        return Member.CreateOriginalValueExpression(this, inputExpression);
    }

    public Expression CreateCurrentValueExpression(Expression inputExpression)
    {
        return Member.CreateCurrentValueExpression(this, inputExpression);
    }
}
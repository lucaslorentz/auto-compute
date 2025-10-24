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

    public Expression CreateOriginalValueExpression(
        Expression inputParameter,
        Expression incrementalContextExpression)
    {
        return Member.CreateOriginalValueExpression(this, inputParameter, incrementalContextExpression);
    }

    public Expression CreateCurrentValueExpression(
        Expression inputParameter,
        Expression incrementalContextExpression)
    {
        return Member.CreateCurrentValueExpression(this, inputParameter, incrementalContextExpression);
    }
}
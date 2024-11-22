using System.Linq.Expressions;

namespace LLL.AutoCompute;

public class ObservedMemberAccess<TMember>(
    Expression expression,
    Expression fromExpression,
    TMember value
) : IObservedMemberAccess<TMember>
    where TMember : IObservedMember<TMember>
{
    public Expression Expression { get; } = expression;
    public Expression FromExpression { get; } = fromExpression;
    public TMember Member { get; } = value;

    public Expression CreateOriginalValueExpression(Expression inputParameter)
    {
        return Member.CreateOriginalValueExpression(this, inputParameter);
    }

    public Expression CreateCurrentValueExpression(Expression inputParameter)
    {
        return Member.CreateCurrentValueExpression(this, inputParameter);
    }

    public Expression CreateIncrementalOriginalValueExpression(
        Expression inputParameter,
        Expression incrementalContextExpression)
    {
        return Member.CreateIncrementalOriginalValueExpression(this, inputParameter, incrementalContextExpression);
    }

    public Expression CreateIncrementalCurrentValueExpression(
        Expression inputParameter,
        Expression incrementalContextExpression)
    {
        return Member.CreateIncrementalCurrentValueExpression(this, inputParameter, incrementalContextExpression);
    }
}

public static class ObserbedMemberAccess
{
    public static ObservedMemberAccess<TMember> Create<TMember>(
        Expression expression,
        Expression fromExpression,
        TMember value)
        where TMember : IObservedMember<TMember>
    {
        return new ObservedMemberAccess<TMember>(expression, fromExpression, value);
    }
}
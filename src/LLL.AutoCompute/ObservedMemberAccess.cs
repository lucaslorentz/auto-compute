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

public class ObservedPropertyAccess(
    Expression expression,
    Expression fromExpression,
    IObservedProperty property
) : ObservedMemberAccess(expression, fromExpression, property), IObservedPropertyAccess
{
    public IObservedProperty Property => property;
}

public class ObservedNavigationAccess(
    Expression expression,
    Expression fromExpression,
    IObservedNavigation navigation
) : ObservedMemberAccess(expression, fromExpression, navigation), IObservedNavigationAccess
{
    public IObservedNavigation Navigation => navigation;
}
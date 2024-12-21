using System.Linq.Expressions;

namespace LLL.AutoCompute;

public class ObservedPropertyAccess(
    Expression expression,
    Expression fromExpression,
    IObservedProperty property
) : ObservedMemberAccess(expression, fromExpression, property), IObservedPropertyAccess
{
    public IObservedProperty Property => property;
}
using System.Linq.Expressions;

namespace LLL.AutoCompute;

public class ObservedNavigationAccess(
    Expression expression,
    Expression fromExpression,
    IObservedNavigation navigation
) : ObservedMemberAccess(expression, fromExpression, navigation), IObservedNavigationAccess
{
    public IObservedNavigation Navigation => navigation;
}
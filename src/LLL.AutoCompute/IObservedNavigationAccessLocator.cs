using System.Linq.Expressions;

namespace LLL.AutoCompute;

public interface IObservedNavigationAccessLocator : IObservedMemberAccessLocator
{
    IObservedNavigationAccess? GetObservedNavigationAccess(Expression node);
}

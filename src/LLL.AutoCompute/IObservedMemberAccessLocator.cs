using System.Linq.Expressions;

namespace LLL.AutoCompute;

public interface IObservedMemberAccessLocator
{
    public IObservedMemberAccess? GetObservedMemberAccess(Expression node)
    {
        if (this is IObservedNavigationAccessLocator nav)
        {
            var access = nav.GetObservedNavigationAccess(node);
            if (access is not null)
                return access;
        }

        if (this is IObservedPropertyAccessLocator prop)
        {
            var access = prop.GetObservedPropertyAccess(node);
            if (access is not null)
                return access;
        }

        return null;
    }
}
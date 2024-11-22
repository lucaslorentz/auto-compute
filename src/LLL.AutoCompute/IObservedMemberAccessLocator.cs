using System.Linq.Expressions;

namespace LLL.AutoCompute;

public interface IObservedMemberAccessLocator
{
    public IObservedMemberAccess<IObservedMember>? GetObservedMemberAccess(Expression node)
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

public interface IObservedMemberAccessLocator<in TInput> : IObservedMemberAccessLocator
{
}

public interface IObservedPropertyAccessLocator : IObservedMemberAccessLocator
{
    IObservedMemberAccess<IObservedProperty>? GetObservedPropertyAccess(Expression node);
}

public interface IObservedPropertyAccessLocator<in TInput>
    : IObservedPropertyAccessLocator, IObservedMemberAccessLocator<TInput>
{
}


public interface IObservedNavigationAccessLocator : IObservedMemberAccessLocator
{
    IObservedMemberAccess<IObservedNavigation>? GetObservedNavigationAccess(Expression node);
}

public interface IObservedNavigationAccessLocator<in TInput>
    : IObservedNavigationAccessLocator, IObservedMemberAccessLocator<TInput>
{
}

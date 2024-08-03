using System.Linq.Expressions;

namespace LLL.AutoCompute;

public interface IEntityMemberAccessLocator
{
    public IEntityMemberAccess<IEntityMember>? GetEntityMemberAccess(Expression node)
    {
        if (this is IEntityNavigationAccessLocator nav)
        {
            var access = nav.GetEntityNavigationAccess(node);
            if (access is not null)
                return access;
        }

        if (this is IEntityPropertyAccessLocator prop)
        {
            var access = prop.GetEntityPropertyAccess(node);
            if (access is not null)
                return access;
        }

        return null;
    }
}

public interface IEntityMemberAccessLocator<in TInput> : IEntityMemberAccessLocator
{
}

public interface IEntityPropertyAccessLocator : IEntityMemberAccessLocator
{
    IEntityMemberAccess<IEntityProperty>? GetEntityPropertyAccess(Expression node);
}

public interface IEntityPropertyAccessLocator<in TInput>
    : IEntityPropertyAccessLocator, IEntityMemberAccessLocator<TInput>
{
}


public interface IEntityNavigationAccessLocator : IEntityMemberAccessLocator
{
    IEntityMemberAccess<IEntityNavigation>? GetEntityNavigationAccess(Expression node);
}

public interface IEntityNavigationAccessLocator<in TInput>
    : IEntityNavigationAccessLocator, IEntityMemberAccessLocator<TInput>
{
}

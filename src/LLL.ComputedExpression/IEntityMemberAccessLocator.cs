using System.Linq.Expressions;

namespace LLL.Computed;

public interface IEntityMemberAccessLocator
{
    IEntityMemberAccess<IEntityMember>? GetEntityMemberAccess(Expression node);
}

public interface IEntityMemberAccessLocator<TMember> : IEntityMemberAccessLocator
{
    new IEntityMemberAccess<TMember>? GetEntityMemberAccess(Expression node);

    IEntityMemberAccess<IEntityMember>? IEntityMemberAccessLocator.GetEntityMemberAccess(Expression node)
    {
        return GetEntityMemberAccess(node) as IEntityMemberAccess<IEntityMember>;
    }
}

public interface IEntityMemberAccessLocator<TMember, in TInput> : IEntityMemberAccessLocator<TMember>
    where TMember : IEntityMember<TMember>
{
}

public interface IAllEntityMemberAccessLocator<in TInput> :
    IEntityMemberAccessLocator<IEntityNavigation, TInput>,
    IEntityMemberAccessLocator<IEntityProperty, TInput>
{
    IEntityMemberAccess<IEntityMember>? IEntityMemberAccessLocator.GetEntityMemberAccess(Expression node)
    {
        IEntityMemberAccess<IEntityMember>? result = null;
        result ??= ((IEntityMemberAccessLocator<IEntityNavigation, TInput>)this).GetEntityMemberAccess(node);
        result ??= ((IEntityMemberAccessLocator<IEntityProperty, TInput>)this).GetEntityMemberAccess(node);
        return result;
    }
}

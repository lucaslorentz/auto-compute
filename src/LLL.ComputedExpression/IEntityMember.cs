using System.Linq.Expressions;

namespace LLL.ComputedExpression;

public interface IEntityMember
{
    string Name { get; }
    IAffectedEntitiesProvider? GetAffectedEntitiesProvider();
    Expression CreateOriginalValueExpression(
        IEntityMemberAccess<IEntityMember> memberAccess,
        Expression inputExpression);
    Expression CreateCurrentValueExpression(
        IEntityMemberAccess<IEntityMember> memberAccess,
        Expression inputExpression);
}


public interface IEntityMember<TMember> : IEntityMember
{
    Expression CreateOriginalValueExpression(
        IEntityMemberAccess<TMember> memberAccess,
        Expression inputExpression);

    Expression CreateCurrentValueExpression(
        IEntityMemberAccess<TMember> memberAccess,
        Expression inputExpression);

    Expression IEntityMember.CreateOriginalValueExpression(
        IEntityMemberAccess<IEntityMember> memberAccess,
        Expression inputExpression)
    {
        if (memberAccess is not IEntityMemberAccess<TMember> memberAccessTyped)
            throw new ArgumentException($"Param {nameof(memberAccess)} should be of type {typeof(IEntityMemberAccess<TMember>)}");

        return CreateOriginalValueExpression(
            memberAccessTyped,
            inputExpression);
    }

    Expression IEntityMember.CreateCurrentValueExpression(
        IEntityMemberAccess<IEntityMember> memberAccess,
        Expression inputExpression)
    {
        if (memberAccess is not IEntityMemberAccess<TMember> memberAccessTyped)
            throw new ArgumentException($"Param {nameof(memberAccess)} should be of type {typeof(IEntityMemberAccess<TMember>)}");

        return CreateCurrentValueExpression(
            memberAccessTyped,
            inputExpression);
    }
}

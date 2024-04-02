using System.Linq.Expressions;

namespace LLL.ComputedExpression;

public interface IEntityMember
{
    string Name { get; }
    IAffectedEntitiesProvider? GetAffectedEntitiesProvider();
    Expression CreatePreviousValueExpression(
        IEntityMemberAccess<IEntityMember> memberAccess,
        Expression inputExpression);
}


public interface IEntityMember<TMember> : IEntityMember
{
    Expression CreatePreviousValueExpression(
        IEntityMemberAccess<TMember> memberAccess,
        Expression inputExpression);

    Expression IEntityMember.CreatePreviousValueExpression(
        IEntityMemberAccess<IEntityMember> memberAccess,
        Expression inputExpression)
    {
        if (memberAccess is not IEntityMemberAccess<TMember> memberAccessTyped)
            throw new ArgumentException($"Param {nameof(memberAccess)} should be of type {typeof(IEntityMemberAccess<TMember>)}");

        return CreatePreviousValueExpression(
            memberAccessTyped,
            inputExpression);
    }
}

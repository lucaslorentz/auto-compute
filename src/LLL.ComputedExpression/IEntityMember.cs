using System.Linq.Expressions;

namespace LLL.Computed;

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
        return CreatePreviousValueExpression(
            (IEntityMemberAccess<TMember>)memberAccess,
            inputExpression);
    }
}

using System.Linq.Expressions;

namespace LLL.Computed;

public interface IEntityMember
{
    string ToDebugString();
    IAffectedEntitiesProvider GetAffectedEntitiesProvider();
    Expression CreatePreviousValueExpression(IEntityMemberAccess<IEntityMember> expression);
}


public interface IEntityMember<TMember> : IEntityMember
{
    Expression CreatePreviousValueExpression(IEntityMemberAccess<TMember> expression);

    Expression IEntityMember.CreatePreviousValueExpression(IEntityMemberAccess<IEntityMember> expression) {
        return CreatePreviousValueExpression(expression);
    }
}

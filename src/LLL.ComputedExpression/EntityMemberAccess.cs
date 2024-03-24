using System.Linq.Expressions;

namespace LLL.Computed;

public class EntityMemberAccess<TMember>(
    Expression fromExpression,
    TMember value
) : IEntityMemberAccess<TMember>
{
    public Expression FromExpression { get; } = fromExpression;
    public TMember Member { get; } = value;
}

public static class EntityMemberAccess
{
    public static EntityMemberAccess<TMember> Create<TMember>(
        Expression fromExpression,
        TMember value)
    {
        return new EntityMemberAccess<TMember>(fromExpression, value);
    }
}
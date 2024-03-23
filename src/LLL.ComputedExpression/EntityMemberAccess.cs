using System.Linq.Expressions;

namespace LLL.Computed;

public class EntityMemberAccess<TMember>(
    Expression fromExpression,
    TMember value,
    Func<Expression, Expression>? previousValueExpressionFactory
) : IEntityMemberAccess<TMember>
{
    public Expression FromExpression { get; } = fromExpression;
    public TMember Member { get; } = value;

    public Expression CreatePreviousValueExpression(Expression expression)
    {
        return (previousValueExpressionFactory ?? throw new Exception("Previous value not supported"))(expression);
    }
}

public static class EntityMemberAccess
{
    public static EntityMemberAccess<TMember> Create<TMember>(
        Expression fromExpression,
        TMember value,
        Func<Expression, Expression>? previousValueExpressionFactory = null)
    {
        return new EntityMemberAccess<TMember>(fromExpression, value, previousValueExpressionFactory);
    }
}
using System.Linq.Expressions;

namespace LLL.ComputedExpression;

public class EntityMemberAccess<TMember>(
    Expression expression,
    Expression fromExpression,
    TMember value
) : IEntityMemberAccess<TMember>
    where TMember : IEntityMember<TMember>
{
    public Expression Expression { get; } = expression;
    public Expression FromExpression { get; } = fromExpression;
    public TMember Member { get; } = value;

    public Expression CreateOriginalValueExpression(Expression inputParameter)
    {
        return Member.CreateOriginalValueExpression(this, inputParameter);
    }

    public Expression CreateCurrentValueExpression(Expression inputParameter)
    {
        return Member.CreateCurrentValueExpression(this, inputParameter);
    }

    public Expression CreateIncrementalOriginalValueExpression(
        Expression inputParameter,
        Expression incrementalContextExpression)
    {
        return Member.CreateIncrementalOriginalValueExpression(this, inputParameter, incrementalContextExpression);
    }

    public Expression CreateIncrementalCurrentValueExpression(
        Expression inputParameter,
        Expression incrementalContextExpression)
    {
        return Member.CreateIncrementalCurrentValueExpression(this, inputParameter, incrementalContextExpression);
    }
}

public static class EntityMemberAccess
{
    public static EntityMemberAccess<TMember> Create<TMember>(
        Expression expression,
        Expression fromExpression,
        TMember value)
        where TMember : IEntityMember<TMember>
    {
        return new EntityMemberAccess<TMember>(expression, fromExpression, value);
    }
}
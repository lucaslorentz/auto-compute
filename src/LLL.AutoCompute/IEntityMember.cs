using System.Linq.Expressions;

namespace LLL.AutoCompute;

public interface IEntityMember
{
    string Name { get; }
    Type InputType { get; }
    string ToDebugString();
    Expression CreateOriginalValueExpression(
        IEntityMemberAccess<IEntityMember> memberAccess,
        Expression inputExpression);
    Expression CreateCurrentValueExpression(
        IEntityMemberAccess<IEntityMember> memberAccess,
        Expression inputExpression);
    Expression CreateIncrementalOriginalValueExpression(
        IEntityMemberAccess<IEntityMember> memberAccess,
        Expression inputExpression,
        Expression incrementalContextExpression);
    Expression CreateIncrementalCurrentValueExpression(
        IEntityMemberAccess<IEntityMember> memberAccess,
        Expression inputExpression,
        Expression incrementalContextExpression);
}


public interface IEntityMember<TMember> : IEntityMember
    where TMember : IEntityMember
{
    Expression CreateOriginalValueExpression(
        IEntityMemberAccess<TMember> memberAccess,
        Expression inputExpression);

    Expression CreateCurrentValueExpression(
        IEntityMemberAccess<TMember> memberAccess,
        Expression inputExpression);

    Expression CreateIncrementalOriginalValueExpression(
        IEntityMemberAccess<TMember> memberAccess,
        Expression inputExpression,
        Expression incrementalContextExpression);

    Expression CreateIncrementalCurrentValueExpression(
        IEntityMemberAccess<TMember> memberAccess,
        Expression inputExpression,
        Expression incrementalContextExpression);

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

    Expression IEntityMember.CreateIncrementalOriginalValueExpression(
        IEntityMemberAccess<IEntityMember> memberAccess,
        Expression inputExpression,
        Expression incrementalContextExpression)
    {
        if (memberAccess is not IEntityMemberAccess<TMember> memberAccessTyped)
            throw new ArgumentException($"Param {nameof(memberAccess)} should be of type {typeof(IEntityMemberAccess<TMember>)}");

        return CreateIncrementalOriginalValueExpression(
            memberAccessTyped,
            inputExpression,
            incrementalContextExpression);
    }

    Expression IEntityMember.CreateIncrementalCurrentValueExpression(
        IEntityMemberAccess<IEntityMember> memberAccess,
        Expression inputExpression,
        Expression incrementalContextExpression)
    {
        if (memberAccess is not IEntityMemberAccess<TMember> memberAccessTyped)
            throw new ArgumentException($"Param {nameof(memberAccess)} should be of type {typeof(IEntityMemberAccess<TMember>)}");

        return CreateIncrementalCurrentValueExpression(
            memberAccessTyped,
            inputExpression,
            incrementalContextExpression);
    }
}

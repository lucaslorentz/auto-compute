using System.Linq.Expressions;

namespace LLL.ComputedExpression;

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
        ComputedExpressionAnalysis analysis,
        IEntityMemberAccess<IEntityMember> memberAccess,
        Expression inputExpression,
        Expression incrementalContextExpression);
    Expression CreateIncrementalCurrentValueExpression(
        ComputedExpressionAnalysis analysis,
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
        ComputedExpressionAnalysis analysis,
        IEntityMemberAccess<TMember> memberAccess,
        Expression inputExpression,
        Expression incrementalContextExpression);

    Expression CreateIncrementalCurrentValueExpression(
        ComputedExpressionAnalysis analysis,
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
        ComputedExpressionAnalysis analysis,
        IEntityMemberAccess<IEntityMember> memberAccess,
        Expression inputExpression,
        Expression incrementalContextExpression)
    {
        if (memberAccess is not IEntityMemberAccess<TMember> memberAccessTyped)
            throw new ArgumentException($"Param {nameof(memberAccess)} should be of type {typeof(IEntityMemberAccess<TMember>)}");

        return CreateIncrementalOriginalValueExpression(
            analysis,
            memberAccessTyped,
            inputExpression,
            incrementalContextExpression);
    }

    Expression IEntityMember.CreateIncrementalCurrentValueExpression(
        ComputedExpressionAnalysis analysis,
        IEntityMemberAccess<IEntityMember> memberAccess,
        Expression inputExpression,
        Expression incrementalContextExpression)
    {
        if (memberAccess is not IEntityMemberAccess<TMember> memberAccessTyped)
            throw new ArgumentException($"Param {nameof(memberAccess)} should be of type {typeof(IEntityMemberAccess<TMember>)}");

        return CreateIncrementalCurrentValueExpression(
            analysis,
            memberAccessTyped,
            inputExpression,
            incrementalContextExpression);
    }
}

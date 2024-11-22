using System.Linq.Expressions;

namespace LLL.AutoCompute;

public interface IObservedMember
{
    string Name { get; }
    Type InputType { get; }
    string ToDebugString();
    Expression CreateOriginalValueExpression(
        IObservedMemberAccess<IObservedMember> memberAccess,
        Expression inputExpression);
    Expression CreateCurrentValueExpression(
        IObservedMemberAccess<IObservedMember> memberAccess,
        Expression inputExpression);
    Expression CreateIncrementalOriginalValueExpression(
        IObservedMemberAccess<IObservedMember> memberAccess,
        Expression inputExpression,
        Expression incrementalContextExpression);
    Expression CreateIncrementalCurrentValueExpression(
        IObservedMemberAccess<IObservedMember> memberAccess,
        Expression inputExpression,
        Expression incrementalContextExpression);
    Task<IReadOnlyCollection<object>> GetAffectedEntitiesAsync(object input, IncrementalContext incrementalContext);
}


public interface IObservedMember<TMember> : IObservedMember
    where TMember : IObservedMember
{
    Expression CreateOriginalValueExpression(
        IObservedMemberAccess<TMember> memberAccess,
        Expression inputExpression);

    Expression CreateCurrentValueExpression(
        IObservedMemberAccess<TMember> memberAccess,
        Expression inputExpression);

    Expression CreateIncrementalOriginalValueExpression(
        IObservedMemberAccess<TMember> memberAccess,
        Expression inputExpression,
        Expression incrementalContextExpression);

    Expression CreateIncrementalCurrentValueExpression(
        IObservedMemberAccess<TMember> memberAccess,
        Expression inputExpression,
        Expression incrementalContextExpression);

    Expression IObservedMember.CreateOriginalValueExpression(
        IObservedMemberAccess<IObservedMember> memberAccess,
        Expression inputExpression)
    {
        if (memberAccess is not IObservedMemberAccess<TMember> memberAccessTyped)
            throw new ArgumentException($"Param {nameof(memberAccess)} should be of type {typeof(IObservedMemberAccess<TMember>)}");

        return CreateOriginalValueExpression(
            memberAccessTyped,
            inputExpression);
    }

    Expression IObservedMember.CreateCurrentValueExpression(
        IObservedMemberAccess<IObservedMember> memberAccess,
        Expression inputExpression)
    {
        if (memberAccess is not IObservedMemberAccess<TMember> memberAccessTyped)
            throw new ArgumentException($"Param {nameof(memberAccess)} should be of type {typeof(IObservedMemberAccess<TMember>)}");

        return CreateCurrentValueExpression(
            memberAccessTyped,
            inputExpression);
    }

    Expression IObservedMember.CreateIncrementalOriginalValueExpression(
        IObservedMemberAccess<IObservedMember> memberAccess,
        Expression inputExpression,
        Expression incrementalContextExpression)
    {
        if (memberAccess is not IObservedMemberAccess<TMember> memberAccessTyped)
            throw new ArgumentException($"Param {nameof(memberAccess)} should be of type {typeof(IObservedMemberAccess<TMember>)}");

        return CreateIncrementalOriginalValueExpression(
            memberAccessTyped,
            inputExpression,
            incrementalContextExpression);
    }

    Expression IObservedMember.CreateIncrementalCurrentValueExpression(
        IObservedMemberAccess<IObservedMember> memberAccess,
        Expression inputExpression,
        Expression incrementalContextExpression)
    {
        if (memberAccess is not IObservedMemberAccess<TMember> memberAccessTyped)
            throw new ArgumentException($"Param {nameof(memberAccess)} should be of type {typeof(IObservedMemberAccess<TMember>)}");

        return CreateIncrementalCurrentValueExpression(
            memberAccessTyped,
            inputExpression,
            incrementalContextExpression);
    }
}

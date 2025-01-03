using System.Linq.Expressions;

namespace LLL.AutoCompute;

public interface IObservedMember
{
    string Name { get; }
    Type InputType { get; }
    string ToDebugString();
    Expression CreateOriginalValueExpression(
        IObservedMemberAccess memberAccess,
        Expression inputExpression);
    Expression CreateCurrentValueExpression(
        IObservedMemberAccess memberAccess,
        Expression inputExpression);
    Expression CreateIncrementalOriginalValueExpression(
        IObservedMemberAccess memberAccess,
        Expression inputExpression,
        Expression incrementalContextExpression);
    Expression CreateIncrementalCurrentValueExpression(
        IObservedMemberAccess memberAccess,
        Expression inputExpression,
        Expression incrementalContextExpression);
}

public interface IObservedMember<in TInput> : IObservedMember
{
    Type IObservedMember.InputType => typeof(TInput);
}
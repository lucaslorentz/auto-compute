using System.Linq.Expressions;

namespace LLL.AutoCompute;

public interface IObservedMember
{
    string Name { get; }
    Type InputType { get; }
    string ToDebugString();
    Expression CreateOriginalValueExpression(
        ObservedMemberAccess memberAccess,
        Expression inputExpression);
    Expression CreateCurrentValueExpression(
        ObservedMemberAccess memberAccess,
        Expression inputExpression);
}

public interface IObservedMember<in TInput> : IObservedMember
    where TInput : IComputedInput
{
    Type IObservedMember.InputType => typeof(TInput);
}
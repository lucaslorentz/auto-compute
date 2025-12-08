using System.Linq.Expressions;

namespace LLL.AutoCompute;

public interface IObservedMember
{
    string Name { get; }
    string ToDebugString();
    Expression CreateOriginalValueExpression(
        ObservedMemberAccess memberAccess,
        Expression inputExpression);
    Expression CreateCurrentValueExpression(
        ObservedMemberAccess memberAccess,
        Expression inputExpression);
}

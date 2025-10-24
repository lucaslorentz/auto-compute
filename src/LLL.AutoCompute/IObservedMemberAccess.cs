using System.Linq.Expressions;

namespace LLL.AutoCompute;

public interface IObservedMemberAccess
{
    Expression Expression { get; }
    Expression FromExpression { get; }
    IObservedMember Member { get; }

    Expression CreateOriginalValueExpression(
        Expression inputParameter,
        Expression incrementalContextExpression);
    Expression CreateCurrentValueExpression(
        Expression inputParameter,
        Expression incrementalContextExpression);
}

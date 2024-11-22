using System.Linq.Expressions;

namespace LLL.AutoCompute;

public interface IObservedMemberAccess<out TMember>
    where TMember : IObservedMember
{
    Expression Expression { get; }
    Expression FromExpression { get; }
    TMember Member { get; }

    Expression CreateOriginalValueExpression(Expression inputParameter);
    Expression CreateCurrentValueExpression(Expression inputParameter);
    Expression CreateIncrementalOriginalValueExpression(
        Expression inputParameter,
        Expression incrementalContextExpression);
    Expression CreateIncrementalCurrentValueExpression(
        Expression inputParameter,
        Expression incrementalContextExpression);
}
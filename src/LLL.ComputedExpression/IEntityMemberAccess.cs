using System.Linq.Expressions;

namespace LLL.ComputedExpression;

public interface IEntityMemberAccess<out TMember>
    where TMember : IEntityMember
{
    Expression Expression { get; }
    Expression FromExpression { get; }
    TMember Member { get; }

    Expression CreateOriginalValueExpression(Expression inputParameter);
    Expression CreateCurrentValueExpression(Expression inputParameter);
    Expression CreateIncrementalOriginalValueExpression(
        ComputedExpressionAnalysis analysis,
        Expression inputParameter,
        Expression incrementalContextExpression);
    Expression CreateIncrementalCurrentValueExpression(
        ComputedExpressionAnalysis analysis,
        Expression inputParameter,
        Expression incrementalContextExpression);
}
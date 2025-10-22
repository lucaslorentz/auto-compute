
using System.Linq.Expressions;

namespace LLL.AutoCompute.Internal.ExpressionVisitors;

public class CollectExpressionsVisitor(
    IList<Expression> matches,
    Func<Expression, bool>? filter
) : ExpressionVisitor
{
    public static IList<Expression> Collect(
        Expression expression,
        Func<Expression, bool>? predicate = null)
    {
        var matches = new List<Expression>();
        var visitor = new CollectExpressionsVisitor(matches, predicate);
        visitor.Visit(expression);
        return matches;
    }

    public override Expression? Visit(Expression? node)
    {
        if (node is not null && (filter is null || filter(node)))
        {
            matches.Add(node);
        }

        return base.Visit(node);
    }
}

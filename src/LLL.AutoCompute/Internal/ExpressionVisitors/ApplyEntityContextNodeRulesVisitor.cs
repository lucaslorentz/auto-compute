using System.Linq.Expressions;

namespace LLL.AutoCompute.Internal.ExpressionVisitors;

internal class ApplyEntityContextNodeRulesVisitor(
    IList<IEntityContextNodeRule> rules,
    IEntityContextRegistry entityContextBuilder
) : ExpressionVisitor
{
    public override Expression? Visit(Expression? node)
    {
        if (node is not null)
        {
            foreach (var rule in rules)
                rule.Apply(node, entityContextBuilder);
        }

        return base.Visit(node);
    }
}

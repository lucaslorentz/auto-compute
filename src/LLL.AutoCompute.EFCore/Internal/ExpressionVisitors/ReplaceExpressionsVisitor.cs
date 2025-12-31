using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace LLL.AutoCompute.EFCore.Metadata.Internal.ExpressionVisitors;

public class ReplaceExpressionsVisitor(
    IReadOnlyDictionary<Expression, Expression> replacements
) : ExpressionVisitor
{
    [return: NotNullIfNotNull(nameof(node))]
    public override Expression? Visit(Expression? node)
    {
        if (node is null)
            return null;

        var expression = base.Visit(node);

        if (replacements.TryGetValue(node, out var replacement))
            return replacement;

        return expression;
    }
}

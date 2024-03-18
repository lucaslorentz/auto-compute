using System.Linq.Expressions;

namespace LLL.Computed.ExpressionVisitors;

internal class TrackEntityChangesVisitor(
    IList<IEntityChangeTracker> entityChangeTrackers,
    IComputedExpressionAnalysis analysis
) : ExpressionVisitor
{
    public override Expression? Visit(Expression? node)
    {
        if (node is not null)
        {
            foreach (var entityChangeTracker in entityChangeTrackers)
                entityChangeTracker.TrackChanges(node, analysis);
        }

        return base.Visit(node);
    }
}

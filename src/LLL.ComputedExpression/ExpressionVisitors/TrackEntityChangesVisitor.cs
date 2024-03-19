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
            {
                var matches = entityChangeTracker.TrackChanges(node);
                foreach (var match in matches)
                {
                    var entityContext = analysis.ResolveEntityContext(match.FromExpression, EntityContextKeys.None);
                    if (entityContext.IsTrackingChanges)
                        entityContext.AddAffectedEntitiesProvider(match.Value);
                }
            }
        }

        return base.Visit(node);
    }
}

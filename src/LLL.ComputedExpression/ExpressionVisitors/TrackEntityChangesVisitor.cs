using System.Linq.Expressions;

namespace LLL.Computed.ExpressionVisitors;

internal class TrackEntityChangesVisitor(
    IComputedExpressionAnalysis analysis,
    ICollection<IEntityMemberAccessLocator> memberAccessLocators
) : ExpressionVisitor
{
    public override Expression? Visit(Expression? node)
    {
        if (node is not null)
        {
            foreach (var memberAccessLocator in memberAccessLocators)
            {
                var memberAccess = memberAccessLocator.GetEntityMemberAccess(node);
                if (memberAccess is not null)
                {
                    var affectedEntitiesProvider = memberAccess.Member.GetAffectedEntitiesProvider();
                    if (affectedEntitiesProvider is not null)
                    {
                        var entityContext = analysis.ResolveEntityContext(memberAccess.FromExpression, EntityContextKeys.None);
                        if (entityContext.IsTrackingChanges)
                            entityContext.AddAffectedEntitiesProvider(memberAccess.Member.GetAffectedEntitiesProvider());
                    }
                }
            }
        }

        return base.Visit(node);
    }
}

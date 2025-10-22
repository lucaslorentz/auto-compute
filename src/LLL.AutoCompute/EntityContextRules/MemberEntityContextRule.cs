using System.Linq.Expressions;
using LLL.AutoCompute.EntityContexts;

namespace LLL.AutoCompute.Internal.ExpressionVisitors;

internal class MemberEntityContextRule(
    IReadOnlyCollection<IObservedMemberAccessLocator> memberAccessLocators
) : IEntityContextNodeRule
{
    public void Apply(
        Expression node,
        IEntityContextRegistry entityContextRegistry)
    {
        foreach (var memberAccessLocator in memberAccessLocators)
        {
            var memberAccess = memberAccessLocator.GetObservedMemberAccess(node);
            if (memberAccess is not null)
            {
                if (memberAccess is IObservedNavigationAccess navigationAccess)
                {
                    var toKey = navigationAccess.Navigation.IsCollection
                        ? EntityContextKeys.Element
                        : EntityContextKeys.None;

                    entityContextRegistry.RegisterPropagation(
                        navigationAccess.FromExpression,
                        EntityContextKeys.None,
                        node,
                        toKey,
                        context => new NavigationEntityContext(node, context, navigationAccess.Navigation));
                }

                entityContextRegistry.RegisterRequiredModifier(
                    memberAccess.FromExpression,
                    EntityContextKeys.None,
                    entityContext =>
                    {
                        if (entityContext.IsTrackingChanges)
                            entityContext.RegisterObservedMember(memberAccess.Member);
                    });
            }
        }
    }
}

using System.Linq.Expressions;

namespace LLL.Computed.EntityContextPropagators;

public class GroupingEntityContextPropagator : IEntityContextPropagator
{
    public void PropagateEntityContext(Expression node, IComputedExpressionAnalysis analysis)
    {
        if (node is MemberExpression memberExpression)
        {
            if (memberExpression.Member.DeclaringType != null
                && memberExpression.Member.DeclaringType.IsConstructedGenericType
                && memberExpression.Member.DeclaringType.GetGenericTypeDefinition() == typeof(IGrouping<,>)
                && memberExpression.Member.Name == "Key"
                && memberExpression.Expression != null)
            {
                analysis.PropagateEntityContext(memberExpression.Expression, EntityContextKeys.Key, node, EntityContextKeys.None);
            }
        }
    }
}

using System.Linq.Expressions;

namespace LLL.AutoCompute.EntityContextPropagators;

public class KeyValuePairEntityContextPropagator : IEntityContextPropagator
{
    public void PropagateEntityContext(Expression node, IComputedExpressionAnalysis analysis)
    {
        if (node is MemberExpression memberExpression)
        {
            if (memberExpression.Member.DeclaringType != null
                && memberExpression.Member.DeclaringType.IsConstructedGenericType
                && memberExpression.Member.DeclaringType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)
                && memberExpression.Member.Name == "Key"
                && memberExpression.Expression != null)
            {
                analysis.PropagateEntityContext(memberExpression.Expression, EntityContextKeys.Key, node, EntityContextKeys.None);
            }
            else if (memberExpression.Member.DeclaringType != null
                && memberExpression.Member.DeclaringType.IsConstructedGenericType
                && memberExpression.Member.DeclaringType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)
                && memberExpression.Member.Name == "Value"
                && memberExpression.Expression != null)
            {
                analysis.PropagateEntityContext(memberExpression.Expression, EntityContextKeys.Value, node, EntityContextKeys.None);
            }
        }
    }
}

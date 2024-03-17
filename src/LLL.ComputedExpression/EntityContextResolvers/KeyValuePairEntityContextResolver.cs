using System.Linq.Expressions;

namespace L3.Computed;

public class KeyValuePairEntityContextResolver : IEntityContextResolver
{
    public IEntityContext? ResolveEntityContext(
        Expression node,
        IComputedExpressionAnalysis analysis,
        string key)
    {
        if (node is MemberExpression memberExpression)
        {
            if (memberExpression.Member.DeclaringType != null
                && memberExpression.Member.DeclaringType.IsConstructedGenericType
                && memberExpression.Member.DeclaringType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)
                && memberExpression.Member.Name == "Key"
                && memberExpression.Expression != null)
            {
                return analysis.ResolveEntityContext(memberExpression.Expression, EntityContextKeys.Key);
            }
            else if (memberExpression.Member.DeclaringType != null
                && memberExpression.Member.DeclaringType.IsConstructedGenericType
                && memberExpression.Member.DeclaringType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)
                && memberExpression.Member.Name == "Value"
                && memberExpression.Expression != null)
            {
                return analysis.ResolveEntityContext(memberExpression.Expression, EntityContextKeys.Value);
            }
        }

        return null;
    }
}

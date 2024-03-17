using System.Linq.Expressions;

namespace L3.Computed;

public class GroupingEntityContextResolver : IEntityContextResolver
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
                && memberExpression.Member.DeclaringType.GetGenericTypeDefinition() == typeof(IGrouping<,>)
                && memberExpression.Member.Name == "Key"
                && memberExpression.Expression != null)
            {
                return analysis.ResolveEntityContext(memberExpression.Expression, EntityContextKeys.Key);
            }
        }

        return null;
    }
}

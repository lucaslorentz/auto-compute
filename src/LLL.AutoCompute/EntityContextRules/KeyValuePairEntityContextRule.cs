using System.Linq.Expressions;

namespace LLL.AutoCompute.EntityContextPropagators;

public class KeyValuePairEntityContextRule : IEntityContextNodeRule
{
    public void Apply(
        Expression node,
        IEntityContextRegistry entityContextRegistry)
    {
        if (node is MemberExpression memberExpression)
        {
            if (memberExpression.Member.DeclaringType != null
                && memberExpression.Member.DeclaringType.IsConstructedGenericType
                && memberExpression.Member.DeclaringType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)
                && memberExpression.Member.Name == "Key"
                && memberExpression.Expression != null)
            {
                entityContextRegistry.RegisterPropagation(
                    memberExpression.Expression,
                    EntityContextKeys.Key,
                    node,
                    EntityContextKeys.None);
            }
            else if (memberExpression.Member.DeclaringType != null
                && memberExpression.Member.DeclaringType.IsConstructedGenericType
                && memberExpression.Member.DeclaringType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)
                && memberExpression.Member.Name == "Value"
                && memberExpression.Expression != null)
            {
                entityContextRegistry.RegisterPropagation(
                    memberExpression.Expression,
                    EntityContextKeys.Value,
                    node,
                    EntityContextKeys.None);
            }
        }
    }
}

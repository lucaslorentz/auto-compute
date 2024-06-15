using System.Linq.Expressions;
using LLL.ComputedExpression.EntityContexts;

namespace LLL.ComputedExpression;

public interface IComputedExpressionAnalysis
{
    EntityContext ResolveEntityContext(Expression node, string key);
    void PropagateEntityContext(Expression fromNode, string fromKey, Expression toNode, string toKey, Func<EntityContext, EntityContext>? mapper = null);
    void PropagateEntityContext((Expression fromNode, string fromKey)[] fromNodesKeys, Expression toNode, string toKey, Func<EntityContext, EntityContext>? mapper = null);
    void AddEntityContextProvider(Expression node, Func<string, EntityContext?> provider);
    void AddMemberAccess(Expression expression, IEntityMemberAccess<IEntityMember> entityMemberAccess);
    void AddIncrementalAction(Action action);
}

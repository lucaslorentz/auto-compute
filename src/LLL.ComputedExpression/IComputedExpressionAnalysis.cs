using System.Linq.Expressions;
using LLL.Computed.EntityContexts;

namespace LLL.Computed;

public interface IComputedExpressionAnalysis
{
    IComputedExpressionAnalyzer Analyzer { get; }
    EntityContext ResolveEntityContext(Expression node, string key);
    void PropagateEntityContext(Expression fromNode, string fromKey, Expression toNode, string toKey, Func<EntityContext, EntityContext>? mapper = null);
    void PropagateEntityContext((Expression fromNode, string fromKey)[] fromNodesKeys, Expression toNode, string toKey, Func<EntityContext, EntityContext>? mapper = null);
    void AddEntityContextProvider(Expression node, Func<string, EntityContext?> provider);
    void AddMemberAccess(Expression expression, IEntityMemberAccess<IEntityMember> entityMemberAccess);
}

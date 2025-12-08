using System.Linq.Expressions;
using System.Reflection;

namespace LLL.AutoCompute.EFCore.Metadata.Internal.ExpressionVisitors;

public class CollectControlledMembersExpressionVisitor : ExpressionVisitor
{
    private readonly HashSet<MemberInfo> _members = [];
    private readonly Type _targetType;

    private CollectControlledMembersExpressionVisitor(Type targetType)
    {
        _targetType = targetType;
    }

    public static IReadOnlySet<MemberInfo> Collect(Expression node, Type targetType)
    {
        var visitor = new CollectControlledMembersExpressionVisitor(targetType);
        visitor.Visit(node);
        return visitor._members;
    }

    protected override Expression VisitNew(NewExpression node)
    {
        if (node.Type.IsAssignableTo(_targetType)
            && node.Members is not null)
        {
            foreach (var member in node.Members)
            {
                _members.Add(member);
            }
        }

        return base.VisitNew(node);
    }

    protected override Expression VisitMemberInit(MemberInitExpression node)
    {
        if (node.NewExpression.Type.IsAssignableTo(_targetType))
        {
            foreach (var binding in node.Bindings)
            {
                _members.Add(binding.Member);
            }
        }

        return base.VisitMemberInit(node);
    }
}

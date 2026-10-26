using System.Linq.Expressions;
using System.Reflection;
using LLL.AutoCompute.EFCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Internal;

internal sealed class ReadStoredAutoComputedValuesVisitor(IModel model) : ExpressionVisitor
{
    private readonly Dictionary<(Type DeclaringType, string Name), FieldInfo> _fields = model.GetEntityTypes()
        .SelectMany(e => e.GetDeclaredProperties())
        .Where(p => p[AutoComputeAnnotationNames.Declarative] is true && p.PropertyInfo is not null && p.FieldInfo is not null)
        .DistinctBy(p => (p.PropertyInfo!.DeclaringType!, p.PropertyInfo.Name))
        .ToDictionary(p => (p.PropertyInfo!.DeclaringType!, p.PropertyInfo.Name), p => p.FieldInfo!);

    protected override Expression VisitMember(MemberExpression node)
        => node.Member is PropertyInfo property && _fields.TryGetValue((property.DeclaringType!, property.Name), out var field)
            ? Expression.Field(Visit(node.Expression), field)
            : base.VisitMember(node);
}

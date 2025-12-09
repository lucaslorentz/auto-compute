using System.Linq.Expressions;
using System.Reflection;
using LLL.AutoCompute.EFCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Internal;

public abstract class EFCoreObservedMember : IObservedMember
{
    private readonly HashSet<ComputedMember> _dependentMembers = [];

    public abstract IPropertyBase Member { get; }

    internal IReadOnlySet<ComputedMember> DependentMembers => _dependentMembers;
    internal bool AddDependentMember(ComputedMember computed)
    {
        return _dependentMembers.Add(computed);
    }

    public abstract string Name { get; }
    public abstract string ToDebugString();

    public Expression CreateCurrentValueExpression(
        ObservedMemberAccess memberAccess,
        Expression inputExpression)
    {
        return Expression.Convert(
            Expression.Call(
                Expression.Constant(this),
                GetType().GetMethod(nameof(GetCurrentValue), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)!,
                inputExpression,
                memberAccess.FromExpression,
                Expression.Lambda(
                    Expression.Convert(
                        memberAccess.Expression,
                        typeof(object)
                    )
                )
            ),
            Member.ClrType
        );
    }

    public Expression CreateOriginalValueExpression(
        ObservedMemberAccess memberAccess,
        Expression inputExpression)
    {
        return Expression.Convert(
            Expression.Call(
                Expression.Constant(this),
                GetType().GetMethod(nameof(GetOriginalValue), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)!,
                inputExpression,
                memberAccess.FromExpression,
                Expression.Lambda(
                    Expression.Convert(
                        memberAccess.Expression,
                        typeof(object)
                    )
                )
            ),
            Member.ClrType
        );
    }

    protected abstract object? GetCurrentValue(ComputedInput input, object ent, Func<object> currentValueGetter);
    protected abstract object? GetOriginalValue(ComputedInput input, object ent, Func<object> currentValueGetter);

    public abstract Task CollectChangesAsync(DbContext dbContext, EFCoreChangeset changes);
    public abstract Task CollectChangesAsync(EntityEntry entityEntry, EFCoreChangeset changes);
}

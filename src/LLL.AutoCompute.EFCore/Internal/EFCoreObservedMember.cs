using System.Linq.Expressions;
using LLL.AutoCompute.EFCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Internal;

public abstract class EFCoreObservedMember : IObservedMember<IEFCoreComputedInput>
{
    private readonly HashSet<ComputedMember> _dependentMembers = [];

    public abstract IPropertyBase Property { get; }
    public Type InputType => typeof(IEFCoreComputedInput);

    internal IReadOnlySet<ComputedMember> DependentMembers => _dependentMembers;
    internal bool AddDependentMember(ComputedMember computed)
    {
        return _dependentMembers.Add(computed);
    }

    public abstract string Name { get; }
    public abstract string ToDebugString();
    public abstract Expression CreateCurrentValueExpression(
        IObservedMemberAccess memberAccess,
        Expression inputExpression);
    public abstract Expression CreateOriginalValueExpression(
        IObservedMemberAccess memberAccess,
        Expression inputExpression);
    public abstract Task CollectChangesAsync(DbContext dbContext, EFCoreChangeset changes);
    public abstract Task CollectChangesAsync(EntityEntry entityEntry, EFCoreChangeset changes);
}
using System.Linq.Expressions;
using LLL.AutoCompute.EFCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Internal;

public abstract class EFCoreObservedMember : IObservedMember<IEFCoreComputedInput>
{
    private readonly HashSet<ComputedBase> _dependents = [];

    public abstract IPropertyBase Property { get; }
    public IReadOnlySet<ComputedBase> Dependents => _dependents;
    public Type InputType => typeof(IEFCoreComputedInput);

    internal bool AddDependent(ComputedBase computed)
    {
        return _dependents.Add(computed);
    }

    public abstract string Name { get; }
    public abstract string ToDebugString();
    public abstract Expression CreateCurrentValueExpression(IObservedMemberAccess memberAccess, Expression inputExpression);
    public abstract Expression CreateIncrementalCurrentValueExpression(IObservedMemberAccess memberAccess, Expression inputExpression, Expression incrementalContextExpression);
    public abstract Expression CreateIncrementalOriginalValueExpression(IObservedMemberAccess memberAccess, Expression inputExpression, Expression incrementalContextExpression);
    public abstract Expression CreateOriginalValueExpression(IObservedMemberAccess memberAccess, Expression inputExpression);
    public abstract Task CollectChangesAsync(DbContext dbContext, EFCoreChangeset changes);
    public abstract Task CollectChangesAsync(EntityEntry entityEntry, EFCoreChangeset changes);
}
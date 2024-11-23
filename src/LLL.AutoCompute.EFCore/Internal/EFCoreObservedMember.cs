using LLL.AutoCompute.EFCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Internal;

public abstract class EFCoreObservedMember
{
    private readonly HashSet<ComputedBase> _dependents = [];

    public abstract IPropertyBase Property { get; }
    public IReadOnlySet<ComputedBase> Dependents => _dependents;

    internal bool AddDependent(ComputedBase computed)
    {
        return _dependents.Add(computed);
    }
}
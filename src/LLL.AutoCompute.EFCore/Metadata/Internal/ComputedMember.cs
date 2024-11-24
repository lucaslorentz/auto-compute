using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Metadata.Internal;

public abstract class ComputedMember : ComputedBase
{
    public abstract IPropertyBase Property { get; }
    public abstract Task Fix(object entity, DbContext dbContext);

    public override string ToDebugString()
    {
        return $"{Property.DeclaringType.Name}.{Property.Name}";
    }
}

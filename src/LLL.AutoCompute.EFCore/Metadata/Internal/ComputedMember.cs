using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore.Metadata.Internal;

public abstract class ComputedMember : ComputedBase
{
    public abstract IPropertyBase Property { get; }
    public abstract Task Fix(object ent, DbContext dbContext);
}

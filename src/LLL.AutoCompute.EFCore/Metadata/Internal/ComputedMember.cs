using Microsoft.EntityFrameworkCore;

namespace LLL.AutoCompute.EFCore.Metadata.Internal;

public abstract class ComputedMember : ComputedBase
{
    public abstract Task Fix(object ent, DbContext dbContext);
}

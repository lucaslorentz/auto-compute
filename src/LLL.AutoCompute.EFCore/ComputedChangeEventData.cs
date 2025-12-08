using Microsoft.EntityFrameworkCore;

namespace LLL.AutoCompute.EFCore;

public class ComputedChangeEventData<TEntity, TChange>
{
    public required DbContext DbContext { get; init; }
    public required IReadOnlyDictionary<TEntity, TChange> Changes { get; init; }
}

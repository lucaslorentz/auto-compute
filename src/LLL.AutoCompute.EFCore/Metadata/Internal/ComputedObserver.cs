using LLL.AutoCompute.EFCore.Internal;
using Microsoft.EntityFrameworkCore;

namespace LLL.AutoCompute.EFCore.Metadata.Internal;

public class ComputedObserver<TEntity, TChange>(
    IUnboundChangesProvider<IEFCoreComputedInput, TEntity, TChange> changesProvider,
    Func<ComputedChangeEventData<TEntity, TChange>, Task> callback
) : Computed(changesProvider)
    where TEntity : class
{
    public override string ToDebugString()
    {
        return "Observer";
    }

    public override async Task<int> Update(DbContext dbContext)
    {
        var numberOfUpdates = 0;
        var input = dbContext.GetComputedInput();
        var changes = await changesProvider.GetChangesAsync(input, null);
        var eventData = new ComputedChangeEventData<TEntity, TChange>
        {
            DbContext = dbContext,
            Changes = changes
        };
        await callback(eventData);
        return numberOfUpdates;
    }
}
using LLL.AutoCompute.EFCore.Internal;
using Microsoft.EntityFrameworkCore;

namespace LLL.AutoCompute.EFCore.Metadata.Internal;

public abstract class ComputedObserver : ComputedBase
{
}

public class ComputedObserver<TEntity, TChange>(
    IUnboundChangesProvider<IEFCoreComputedInput, TEntity, TChange> changesProvider,
    Func<ComputedChangeEventData<TEntity, TChange>, Task> callback
) : ComputedObserver
    where TEntity : class
{
    public override IUnboundChangesProvider<IEFCoreComputedInput, TEntity, TChange> ChangesProvider => changesProvider;

    public override string ToDebugString()
    {
        return "ComputedObserver";
    }

    public override async Task<UpdateChanges> Update(DbContext dbContext)
    {
        var input = dbContext.GetComputedInput();
        var changes = await changesProvider.GetChangesAsync(input, null);
        if (changes.Count > 0)
        {
            var eventData = new ComputedChangeEventData<TEntity, TChange>
            {
                DbContext = dbContext,
                Changes = changes
            };
            dbContext.GetComputedPostSaveActionQueue().Enqueue(async () =>
            {
                await callback(eventData);
            });
        }
        return new UpdateChanges();
    }
}
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LLL.AutoCompute.EFCore;

public class ComputedInterceptor(bool updateComputedsOnSave) : ISaveChangesInterceptor
{
    public async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (updateComputedsOnSave)
        {
            await eventData.Context!.UpdateComputedsAsync();
        }

        return result;
    }

    public InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        return SavingChangesAsync(eventData, result).GetAwaiter().GetResult();
    }

    public async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var postSaveActionsQueue = eventData.Context?.GetComputedPostSaveActionQueue();
        if (postSaveActionsQueue is not null)
        {
            while (postSaveActionsQueue.TryDequeue(out var postSaveAction))
                await postSaveAction();
        }
        return result;
    }

    public int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        return SavedChangesAsync(eventData, result).GetAwaiter().GetResult();
    }
}

using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LLL.AutoCompute.EFCore;

public class ComputedSaveChangesInterceptor(
    bool updateComputedsOnSave,
    bool notifyObserversOnSave)
    : ISaveChangesInterceptor
{
    private static readonly ConditionalWeakTable<DbContext, IComputedObserversNotifier> _computedObserversNotifiers = [];

    public async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (updateComputedsOnSave)
        {
            await eventData.Context!.UpdateComputedsAsync();
        }

        if (notifyObserversOnSave)
        {
            var observersNotifier = await eventData.Context!.CreateObserversNotifier();

            _computedObserversNotifiers.AddOrUpdate(eventData.Context!, observersNotifier);
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
        if (_computedObserversNotifiers.TryGetValue(eventData.Context!, out var computedObserversNotifier))
        {
            _computedObserversNotifiers.Remove(eventData.Context!);

            await computedObserversNotifier.Notify();
        }

        return result;
    }

    public int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        return SavedChangesAsync(eventData, result).GetAwaiter().GetResult();
    }
}

using System.Runtime.CompilerServices;
using LLL.ComputedExpression.EFCore.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LLL.ComputedExpression.EFCore;

public class ComputedInterceptor : ISaveChangesInterceptor
{
    private readonly ConditionalWeakTable<DbContext, List<Func<Task>>> _updates = [];

    private class RecursionCounter
    {
        public int Value { get; set; }
    }

    public async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var dbContext = eventData.Context!;

        if (dbContext.Model.FindRuntimeAnnotationValue(ComputedAnnotationNames.Updaters) is not IEnumerable<ComputedUpdater> updaters)
            throw new Exception($"Cannot find runtime annotation {ComputedAnnotationNames.Updaters} for model");

        await dbContext.WithoutAutoDetectChangesAsync(async () =>
        {
            dbContext.ChangeTracker.DetectChanges();

            if (!dbContext.ChangeTracker.HasChanges())
                return;

            var updates = _updates.GetOrCreateValue(dbContext);

            foreach (var updater in updaters)
            {
                var update = await updater(dbContext);
                if (update is not null)
                    updates.Add(update);
            }

            var observers = dbContext.GetComputedObservers();
            foreach (var observer in observers)
            {
                var update = await observer(dbContext);
                if (update is not null)
                    updates.Add(update);
            }
        });

        return result;
    }

    public InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        return SavingChangesAsync(eventData, result, default).GetAwaiter().GetResult();
    }

    public async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default(CancellationToken))
    {
        var dbContext = eventData.Context!;

        var updates = _updates.GetOrCreateValue(dbContext);
        foreach (var update in updates)
            await update();

        updates.Clear();

        return result;
    }

    public int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        return SavedChangesAsync(eventData, result, default).GetAwaiter().GetResult();
    }
}

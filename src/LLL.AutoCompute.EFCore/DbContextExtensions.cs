using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using LLL.AutoCompute.ChangesProviders;
using LLL.AutoCompute.EFCore.Caching;
using LLL.AutoCompute.EFCore.Internal;
using LLL.AutoCompute.EFCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;

namespace LLL.AutoCompute.EFCore;

public static class DbContextExtensions
{
    public static DbContextOptionsBuilder UseAutoCompute(
        this DbContextOptionsBuilder optionsBuilder,
        Action<ComputedOptionsBuilder>? configureOptions = null)
    {
        var builder = new ComputedOptionsBuilder(optionsBuilder);
        configureOptions?.Invoke(builder);
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(builder.Build());
        return optionsBuilder;
    }

    public static async Task<IReadOnlyDictionary<TEntity, TChange>> GetChangesAsync<TEntity, TValue, TChange>(
        this DbContext dbContext,
        Expression<Func<TEntity, TValue>> computedExpression,
        Expression<Func<TEntity, bool>>? filterExpression,
        ChangeCalculationSelector<TValue, TChange> calculationSelector)
        where TEntity : class
    {
        var changesProvider = dbContext.GetChangesProvider(
            computedExpression,
            filterExpression,
            calculationSelector);

        return await changesProvider.GetChangesAsync();
    }

    public static IChangesProvider<TEntity, TChange> GetChangesProvider<TEntity, TValue, TChange>(
        this DbContext dbContext,
        Expression<Func<TEntity, TValue>> computedExpression,
        Expression<Func<TEntity, bool>>? filterExpression,
        ChangeCalculationSelector<TValue, TChange> calculationSelector)
        where TEntity : class
    {
        filterExpression ??= static e => true;

        var changeCalculation = calculationSelector(ChangeCalculations<TValue>.Instance);

        var key = (
            ComputedExpression: new ExpressionCacheKey(computedExpression, ExpressionEqualityComparer.Instance),
            filterExpression: new ExpressionCacheKey(filterExpression, ExpressionEqualityComparer.Instance),
            ChangeCalculation: changeCalculation
        );

        var concurrentCreationCache = dbContext.GetService<IConcurrentCreationCache>();

        var analyzer = dbContext.Model.GetComputedExpressionAnalyzerOrThrow();

        var unboundChangesProvider = concurrentCreationCache.GetOrCreate(
            key,
            k => analyzer.CreateChangesProvider(
                computedExpression,
                filterExpression ?? (x => true),
                changeCalculation)
        );

        return new ChangesProvider<IEFCoreComputedInput, TEntity, TChange>(
            unboundChangesProvider,
            dbContext.GetComputedInput(),
            new ChangeMemory<TEntity, TChange>()
        );
    }

    public static async Task<int> UpdateComputedsAsync(this DbContext dbContext)
    {
        return await WithoutAutoDetectChangesAsync(dbContext, async () =>
        {
            dbContext.ChangeTracker.DetectChanges();

            // TODO: Separate computeds from observers
            var sortedComputeds = dbContext.Model.GetSortedComputedsOrThrow();

            var computedsAndPriority = sortedComputeds
                .Select((computed, priority) => (computed, priority));

            var priorities = computedsAndPriority.ToDictionary(x => x.computed, x => x.priority);
            var queue = new PriorityQueue<ComputedBase, int>(computedsAndPriority);
            var queuedItems = sortedComputeds.ToHashSet();
            var allChanges = new UpdateChanges();

            while (queue.TryDequeue(out var computed, out var _)
                && queuedItems.Remove(computed))
            {
                var changes = await computed.Update(dbContext);

                if (changes.Count == 0)
                    continue;

                changes.MergeIntoAndDetectCycles(allChanges);

                foreach (var affectedComputed in changes.GetAffectedComputeds())
                {
                    if (queuedItems.Add(affectedComputed))
                        queue.Enqueue(affectedComputed, priorities[affectedComputed]);
                }

                dbContext.ChangeTracker.DetectChanges();
            }

            return allChanges.Count;
        });
    }

    private static readonly ConditionalWeakTable<DbContext, IEFCoreComputedInput> _inputs = [];
    internal static IEFCoreComputedInput GetComputedInput(this DbContext dbContext)
    {
        return _inputs.GetValue(dbContext, static k => new EFCoreComputedInput(k));
    }

    private static readonly ConditionalWeakTable<DbContext, ConcurrentQueue<Func<Task>>> _postSaveActionsQueues = [];
    internal static ConcurrentQueue<Func<Task>> GetComputedPostSaveActionQueue(this DbContext context)
    {
        return _postSaveActionsQueues.GetValue(context, k => []);
    }

    internal static async Task<T> WithoutAutoDetectChangesAsync<T>(
        this DbContext dbContext,
        Func<Task<T>> func)
    {
        var autoDetectChanges = dbContext.ChangeTracker.AutoDetectChangesEnabled;
        try
        {
            dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
            return await func();
        }
        finally
        {
            dbContext.ChangeTracker.AutoDetectChangesEnabled = autoDetectChanges;
        }
    }
}

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using LLL.AutoCompute.ChangesProviders;
using LLL.AutoCompute.EFCore.Caching;
using LLL.AutoCompute.EFCore.Internal;
using LLL.AutoCompute.EFCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
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

    public static EFCoreChangesProvider<TEntity, TChange> GetChangesProvider<TEntity, TValue, TChange>(
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

        var entityType = dbContext.Model.FindEntityType(typeof(TEntity))
            ?? throw new Exception($"No EntityType found for {typeof(TEntity)}");

        var unboundChangesProvider = concurrentCreationCache.GetOrCreate(
            key,
            k => analyzer.CreateChangesProvider(
                entityType.GetOrCreateObservedEntityType(),
                computedExpression,
                filterExpression ?? (x => true),
                changeCalculation)
        );

        return new EFCoreChangesProvider<TEntity, TChange>(
            unboundChangesProvider,
            dbContext
        );
    }

    public static async Task<int> UpdateComputedsAsync(this DbContext dbContext)
    {
        return await WithoutAutoDetectChangesAsync(dbContext, async () =>
        {
            dbContext.ChangeTracker.DetectChanges();

            var sortedComputeds = dbContext.Model.GetSortedComputedsOrThrow();

            var changesToProcess = new EFCoreChangeset();

            var observedMembers = sortedComputeds.SelectMany(x => x.ObservedMembers).ToHashSet();
            foreach (var observedMember in observedMembers)
                await observedMember.CollectChangesAsync(dbContext, changesToProcess);

            var updates = new EFCoreChangeset();

            var visitedComputeds = new HashSet<ComputedBase>();

            await UpdateComputedsAsync(sortedComputeds.ToHashSet(), changesToProcess);

            return updates.Count;

            async Task UpdateComputedsAsync(
                IReadOnlySet<ComputedBase> targetComputeds,
                EFCoreChangeset changesToProcess)
            {
                foreach (var computed in sortedComputeds)
                {
                    if (!targetComputeds.Contains(computed))
                        continue;

                    visitedComputeds.Add(computed);

                    var input = new EFCoreComputedInput(dbContext, changesToProcess);

                    var newChanges = await computed.Update(input);

                    if (newChanges.Count == 0)
                        continue;

                    // Detect new changes
                    dbContext.ChangeTracker.DetectChanges();

                    // Register changes in updates, tracking for cyclic updates
                    newChanges.MergeInto(updates, true);

                    // Re-update affected computeds that were already updated
                    var computedsToReUpdate = newChanges.GetAffectedComputeds(visitedComputeds);
                    if (computedsToReUpdate.Count != 0)
                        await UpdateComputedsAsync(computedsToReUpdate, newChanges);

                    // Merge new changes into changesToProcess, to make next computeds in the loop aware of the new changes
                    newChanges.MergeInto(changesToProcess, false);
                }
            }
        });
    }

    [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Optimisation to not create unecessary EntityEntry")]
    internal static IEnumerable<EntityEntry> EntityEntriesOfType(this DbContext dbContext, ITypeBase entityType)
    {
        if (dbContext.ChangeTracker.AutoDetectChangesEnabled)
            dbContext.ChangeTracker.DetectChanges();

        var dependencies = dbContext.GetDependencies();
        return dependencies.StateManager
            .Entries
            .Where(e => e.EntityType == entityType)
            .Select(e => new EntityEntry(e));
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

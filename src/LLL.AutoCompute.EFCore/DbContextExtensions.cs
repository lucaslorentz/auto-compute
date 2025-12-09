using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
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
        ChangeCalculatorSelector<TValue, TChange> calculationSelector)
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
        ChangeCalculatorSelector<TValue, TChange> calculationSelector)
        where TEntity : class
    {
        filterExpression ??= static e => true;

        var changeCalculator = calculationSelector(ChangeCalculatorFactory<TValue>.Instance);

        var key = (
            ComputedExpression: new ExpressionCacheKey(computedExpression, ExpressionEqualityComparer.Instance),
            filterExpression: new ExpressionCacheKey(filterExpression, ExpressionEqualityComparer.Instance),
            ChangeCalculator: changeCalculator
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
                changeCalculator)
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

            var sortedComputedMembers = dbContext.Model.GetAllComputedMembers();

            var changesToProcess = new EFCoreChangeset();

            var observedMembers = sortedComputedMembers.SelectMany(x => x.ObservedMembers).ToHashSet();
            foreach (var observedMember in observedMembers)
                await observedMember.CollectChangesAsync(dbContext, changesToProcess);
            
            var observedEntityTypes = sortedComputedMembers.SelectMany(x => x.ObservedEntityTypes).ToHashSet();
            foreach(var observedEntityType in observedEntityTypes)
                await observedEntityType.CollectChangesAsync(dbContext, changesToProcess);

            var updates = new EFCoreChangeset();

            var visitedComputedMembers = new HashSet<ComputedMember>();

            await UpdateComputedsAsync(sortedComputedMembers.ToHashSet(), changesToProcess);

            return updates.Count;

            async Task UpdateComputedsAsync(
                IReadOnlySet<ComputedMember> targetComputeds,
                EFCoreChangeset changesToProcess)
            {
                foreach (var computed in sortedComputedMembers)
                {
                    if (!targetComputeds.Contains(computed))
                        continue;

                    visitedComputedMembers.Add(computed);

                    var input = new ComputedInput()
                        .Set(dbContext)
                        .Set(changesToProcess);

                    var newChanges = await computed.Update(input);

                    if (newChanges.Count == 0)
                        continue;

                    // Detect new changes
                    dbContext.ChangeTracker.DetectChanges();

                    // Register changes in updates, tracking for cyclic updates
                    newChanges.MergeInto(updates, true);

                    // Re-update affected computeds that were already updated
                    var computedsToReUpdate = newChanges.GetAffectedComputedMembers(visitedComputedMembers);
                    if (computedsToReUpdate.Count != 0)
                        await UpdateComputedsAsync(computedsToReUpdate, newChanges);

                    // Merge new changes into changesToProcess, to make next computeds in the loop aware of the new changes
                    newChanges.MergeInto(changesToProcess, false);
                }
            }
        });
    }

    public static async Task<IComputedObserversNotifier> CreateObserversNotifier(this DbContext dbContext)
    {
        return await WithoutAutoDetectChangesAsync(dbContext, async () =>
        {
            dbContext.ChangeTracker.DetectChanges();

            var allComputedObservers = dbContext.Model.GetAllComputedObservers();

            var changesToProcess = new EFCoreChangeset();

            var observedMembers = allComputedObservers.SelectMany(x => x.ObservedMembers).ToHashSet();
            foreach (var observedMember in observedMembers)
                await observedMember.CollectChangesAsync(dbContext, changesToProcess);

            var observedEntityTypes = allComputedObservers.SelectMany(x => x.ObservedEntityTypes).ToHashSet();
            foreach(var observedEntityType in observedEntityTypes)
                await observedEntityType.CollectChangesAsync(dbContext, changesToProcess);

            var input = new ComputedInput()
                .Set(dbContext)
                .Set(changesToProcess);

            var computedObserversNotifier = new ComputedObserversNotifier();

            foreach (var computed in allComputedObservers)
            {
                var notifier = await computed.CreateObserverNotifier(input);
                if (notifier is not null)
                    computedObserversNotifier.AddNotification(notifier);
            }

            return computedObserversNotifier;
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
            .Where(e => entityType.IsAssignableFrom(e.EntityType))
            .Select(e => new EntityEntry(e));
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

    public static IQueryable<TEntity> CreateConsistencyQuery<TEntity>(
        this DbContext dbContext,
        IEntityType entityType,
        DateTime since)
        where TEntity : class
    {
        IQueryable<TEntity> query = dbContext.Set<TEntity>(entityType.Name);

        var consistencyFilter = entityType.GetConsistencyFilter();
        if (consistencyFilter is not null)
        {
            var analyzer = dbContext.Model.GetComputedExpressionAnalyzerOrThrow();

            var preparedConsistencyFilter = Expression.Lambda<Func<TEntity, bool>>(
                ReplacingExpressionVisitor.Replace(
                    consistencyFilter.Parameters[1],
                    Expression.Constant(since),
                    consistencyFilter.Body
                ),
                consistencyFilter.Parameters[0]);

            preparedConsistencyFilter = (Expression<Func<TEntity, bool>>)analyzer.RunExpressionModifiers(preparedConsistencyFilter);

            query = query.Where(preparedConsistencyFilter);
        }

        return query;
    }
}

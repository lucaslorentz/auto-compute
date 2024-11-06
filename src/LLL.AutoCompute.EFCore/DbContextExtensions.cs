using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using LLL.AutoCompute.ChangesProviders;
using LLL.AutoCompute.EFCore.Internal;
using LLL.AutoCompute.EFCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

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
        var analyzer = dbContext.Model.GetComputedExpressionAnalyzerOrThrow();

        var changeCalculation = calculationSelector(ChangeCalculations<TValue>.Instance);

        var unboundChangesProvider = analyzer.GetChangesProvider(
            computedExpression,
            filterExpression,
            changeCalculation);

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
            var totalChanges = 0;

            dbContext.ChangeTracker.DetectChanges();

            var sortedComputeds = dbContext.Model.GetSortedComputedsOrThrow();

            foreach (var computed in sortedComputeds)
            {
                var changes = await computed.Update(dbContext);

                if (changes > 0)
                {
                    totalChanges += changes;
                    dbContext.ChangeTracker.DetectChanges();
                }
            }

            return totalChanges;
        });
    }

    private static readonly ConditionalWeakTable<DbContext, IEFCoreComputedInput> _inputs = [];
    internal static IEFCoreComputedInput GetComputedInput(this DbContext dbContext)
    {
        return _inputs.GetValue(dbContext, static k => new EFCoreComputedInput(k));
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

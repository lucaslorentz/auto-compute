using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using LLL.AutoCompute.ChangesProviders;
using LLL.AutoCompute.EFCore.Internal;
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
        Expression<ChangeCalculationSelector<TValue, TChange>> calculationSelector)
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
        Expression<ChangeCalculationSelector<TValue, TChange>> calculationSelector)
        where TEntity : class
    {
        var analyzer = GetComputedExpressionAnalyzer(dbContext);

        var unboundChangesProvider = analyzer.GetChangesProvider(
            computedExpression,
            filterExpression,
            calculationSelector);

        return new ChangesProvider<IEFCoreComputedInput, TEntity, TChange>(
            unboundChangesProvider,
            dbContext.GetComputedInput(),
            new ChangeMemory<TEntity, TChange>()
        );
    }

    public static async Task<int> UpdateComputedsAsync(this DbContext dbContext)
    {
        if (dbContext.Model.FindRuntimeAnnotationValue(ComputedAnnotationNames.SortedProperties) is not IEnumerable<ComputedProperty> computedProperties)
            throw new Exception($"Cannot find runtime annotation {ComputedAnnotationNames.SortedProperties} for model");

        return await WithoutAutoDetectChangesAsync(dbContext, async () =>
        {
            var totalChanges = 0;

            dbContext.ChangeTracker.DetectChanges();

            foreach (var computedProperty in computedProperties)
            {
                var changes = await computedProperty.Update(dbContext);

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

    public static IComputedExpressionAnalyzer<IEFCoreComputedInput> GetComputedExpressionAnalyzer(this DbContext dbContext)
    {
        return dbContext.Model.GetComputedExpressionAnalyzer();
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

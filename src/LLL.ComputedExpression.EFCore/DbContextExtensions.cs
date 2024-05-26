using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using LLL.ComputedExpression.ChangesProviders;
using LLL.ComputedExpression.EFCore.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace LLL.ComputedExpression.EFCore;

public static class DbContextExtensions
{
    public static DbContextOptionsBuilder UseComputeds(
        this DbContextOptionsBuilder optionsBuilder,
        Action<ComputedOptionsBuilder>? configureOptions = null)
    {
        var builder = new ComputedOptionsBuilder(optionsBuilder);
        configureOptions?.Invoke(builder);
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(builder.Build());
        return optionsBuilder;
    }

    public static void ObserveComputedChanges<TEntity, TValue, TResult>(
        this DbContext dbContext,
        Expression<Func<TEntity, TValue>> computedExpression,
        Expression<Func<TEntity, bool>>? filterExpression,
        Expression<ChangeCalculationSelector<TValue, TResult>> calculationSelector,
        Func<IReadOnlyDictionary<TEntity, TResult>, Task> callback)
        where TEntity : class
    {
        var unboundChangesProvider = GetUnboundChangesProvider(dbContext, computedExpression, filterExpression, calculationSelector);

        if (unboundChangesProvider is null)
            return;

        var computedObservers = GetComputedObservers(dbContext);
        computedObservers.Add(async (dbContext) =>
        {
            var changes = await unboundChangesProvider.GetChangesAsync(dbContext.GetComputedInput(), new ChangeMemory<TEntity, TResult>());
            if (changes.Count == 0)
                return null;

            return async () => await callback(changes);
        });
    }

    public static async Task<IReadOnlyDictionary<TEntity, TChange>> GetChangesAsync<TEntity, TValue, TChange>(
        this DbContext dbContext,
        Expression<Func<TEntity, TValue>> computedExpression,
        Expression<Func<TEntity, bool>>? filterExpression,
        Expression<ChangeCalculationSelector<TValue, TChange>> calculationSelector)
        where TEntity : class
    {
        var unboundChangesProvider = GetUnboundChangesProvider(dbContext, computedExpression, filterExpression, calculationSelector);

        if (unboundChangesProvider is null)
            return ImmutableDictionary<TEntity, TChange>.Empty;

        return await unboundChangesProvider.GetChangesAsync(dbContext.GetComputedInput(), new ChangeMemory<TEntity, TChange>());
    }

    public static IChangesProvider<TEntity, TChange>? GetChangesProvider<TEntity, TValue, TChange>(
        this DbContext dbContext,
        Expression<Func<TEntity, TValue>> computedExpression,
        Expression<Func<TEntity, bool>>? filterExpression,
        Expression<ChangeCalculationSelector<TValue, TChange>> calculationSelector)
        where TEntity : class
    {
        var unboundChangesProvider = GetUnboundChangesProvider(dbContext, computedExpression, filterExpression, calculationSelector);

        if (unboundChangesProvider is null)
            return null;

        return new ChangesProvider<IEFCoreComputedInput, TEntity, TChange>(
            unboundChangesProvider,
            dbContext.GetComputedInput(),
            new ChangeMemory<TEntity, TChange>()
        );
    }

    public static int SaveAllChanges(this DbContext dbContext)
    {
        var entriesWritten = 0;

        dbContext.WithoutAutoDetectChanges(() =>
        {
            for (var i = 0; i < 10; i++)
            {
                dbContext.ChangeTracker.DetectChanges();

                if (!dbContext.ChangeTracker.HasChanges())
                    return;

                entriesWritten += dbContext.SaveChanges();
            }
        });

        return entriesWritten;
    }

    public static async Task<int> SaveAllChangesAsync(
        this DbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var entriesWritten = 0;

        await dbContext.WithoutAutoDetectChangesAsync(async () =>
        {
            for (var i = 0; i < 10; i++)
            {
                dbContext.ChangeTracker.DetectChanges();

                if (!dbContext.ChangeTracker.HasChanges())
                    return;

                entriesWritten += await dbContext.SaveChangesAsync(cancellationToken);
            }
        });

        return entriesWritten;
    }

    private static IUnboundChangesProvider<IEFCoreComputedInput, TEntity, TChange>? GetUnboundChangesProvider<TEntity, TValue, TChange>(
        DbContext dbContext,
        Expression<Func<TEntity, TValue>> computedExpression,
        Expression<Func<TEntity, bool>>? filterExpression,
        Expression<ChangeCalculationSelector<TValue, TChange>> calculationSelector)
        where TEntity : class
    {
        var analyzer = GetComputedExpressionAnalyzer(dbContext);

        return analyzer.GetChangesProvider(
            computedExpression,
            filterExpression,
            calculationSelector);
    }

    private static readonly ConditionalWeakTable<DbContext, IEFCoreComputedInput> _inputs = [];
    internal static IEFCoreComputedInput GetComputedInput(this DbContext dbContext)
    {
        return _inputs.GetValue(dbContext, static k => new EFCoreComputedInput(k));
    }

    internal static IComputedExpressionAnalyzer<IEFCoreComputedInput> GetComputedExpressionAnalyzer(this DbContext dbContext)
    {
        var annotationValue = dbContext.Model.FindRuntimeAnnotation(ComputedAnnotationNames.ExpressionAnalyzer)?.Value;

        if (annotationValue is not IComputedExpressionAnalyzer<IEFCoreComputedInput> analyzer)
            throw new Exception($"Cannot find {ComputedAnnotationNames.ExpressionAnalyzer} for model");

        return analyzer;
    }

    private static readonly ConditionalWeakTable<DbContext, ConcurrentBag<ComputedUpdater>> _computedObservers = [];
    internal static ConcurrentBag<ComputedUpdater> GetComputedObservers(this DbContext dbContext)
    {
        return _computedObservers.GetOrCreateValue(dbContext);
    }

    internal static void WithoutAutoDetectChanges(
        this DbContext dbContext,
        Action func)
    {
        var autoDetectChanges = dbContext.ChangeTracker.AutoDetectChangesEnabled;
        try
        {
            dbContext.ChangeTracker.AutoDetectChangesEnabled = false;

            func();
        }
        finally
        {
            dbContext.ChangeTracker.AutoDetectChangesEnabled = autoDetectChanges;
        }
    }

    internal static async Task WithoutAutoDetectChangesAsync(
        this DbContext dbContext,
        Func<Task> func)
    {
        var autoDetectChanges = dbContext.ChangeTracker.AutoDetectChangesEnabled;
        try
        {
            dbContext.ChangeTracker.AutoDetectChangesEnabled = false;

            await func();
        }
        finally
        {
            dbContext.ChangeTracker.AutoDetectChangesEnabled = autoDetectChanges;
        }
    }
}

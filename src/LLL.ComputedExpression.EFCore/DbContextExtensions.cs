using System.Collections.Immutable;
using System.Linq.Expressions;
using LLL.ComputedExpression.Caching;
using LLL.ComputedExpression.EFCore.Internal;
using LLL.ComputedExpression.Incremental;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;

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

    public static IAffectedEntitiesProvider? GetAffectedEntitiesProvider(this DbContext dbContext, LambdaExpression computedExpression)
    {
        var analyzer = GetComputedExpressionAnalyzer(dbContext);

        var cache = dbContext.GetService<IConcurrentCreationCache>();

        var cacheKey = (
            Analyzer: analyzer,
            Puropse: "AffectedEntities",
            ExpressionKey: new ExpressionCacheKey(computedExpression, ExpressionEqualityComparer.Instance)
        );

        return cache.GetOrCreate(
            cacheKey,
            static (k) => k.Analyzer.CreateAffectedEntitiesProvider((LambdaExpression)k.ExpressionKey.Expression));
    }

    public static async Task<IReadOnlyCollection<TEntity>> GetAffectedEntitiesAsync<TEntity, P>(
        this DbContext dbContext, Expression<Func<TEntity, P>> computedExpression)
    {
        var affectedEntitiesProvider = dbContext.GetAffectedEntitiesProvider(computedExpression);
        if (affectedEntitiesProvider is null)
            return [];

        var affectedEntities = await affectedEntitiesProvider.GetAffectedEntitiesAsync(new EFCoreComputedInput(dbContext));
        return affectedEntities.OfType<TEntity>().ToArray();
    }

    public static IChangesProvider? GetChangesProviderAsync<TEntity, P>(
        this DbContext dbContext, Expression<Func<TEntity, P>> computedExpression)
        where TEntity : notnull
    {
        var analyzer = GetComputedExpressionAnalyzer(dbContext);

        var cache = dbContext.GetService<IConcurrentCreationCache>();

        var cacheKey = (
            Analyzer: analyzer,
            Purpose: "ChangesProvider",
            ExpressionKey: new ExpressionCacheKey(computedExpression, ExpressionEqualityComparer.Instance)
        );

        return cache.GetOrCreate(
            cacheKey,
            static (k) => k.Analyzer.GetChangesProvider((LambdaExpression)k.ExpressionKey.Expression));
    }

    public static async Task<IReadOnlyDictionary<TEntity, (P? originalValue, P? newValue)>> GetChangesAsync<TEntity, P>(
        this DbContext dbContext, Expression<Func<TEntity, P>> computedExpression)
        where TEntity : notnull
    {
        var changesProvider = dbContext.GetChangesProviderAsync(computedExpression);
        if (changesProvider is null)
            return ImmutableDictionary<TEntity, (P?, P?)>.Empty;

        var changes = await changesProvider.GetChangesAsync(new EFCoreComputedInput(dbContext));
        return changes.ToDictionary(
            kv => (TEntity)kv.Key,
            kv => ((P?)kv.Value.OriginalValue, (P?)kv.Value.NewValue));
    }

    public static async Task<IReadOnlyDictionary<TEntity, V>> GetIncrementalChanges<TEntity, V>(
        this DbContext dbContext, IIncrementalComputed<TEntity, V> incrementalComputed)
        where TEntity : notnull
        where V : notnull
    {
        var analyzer = GetComputedExpressionAnalyzer(dbContext);

        var cache = dbContext.GetService<IConcurrentCreationCache>();

        var cacheKey = (
            "IncrementalChangesProvider",
            incrementalComputed,
            analyzer
        );

        var incrementalChangeProvider = cache.GetOrCreate(
            cacheKey,
            static (k) => k.analyzer.CreateIncrementalChangesProvider(
                k.incrementalComputed));

        var input = new EFCoreComputedInput(dbContext);

        var changes = await incrementalChangeProvider.GetIncrementalChangesAsync(input);
        return changes.ToDictionary(kv => (TEntity)kv.Key, kv => (V)kv.Value!);
    }

    public static async Task<int> UpdateComputedsAsync(this DbContext dbContext)
    {
        if (dbContext.Model.FindRuntimeAnnotationValue(ComputedAnnotationNames.Updaters) is not List<Func<DbContext, Task<int>>> updaters)
            throw new Exception($"Cannot find runtime annotation {ComputedAnnotationNames.Updaters} for model");

        var totalChanges = 0;
        for (var i = 0; i < 10; i++)
        {
            var changes = 0;

            foreach (var update in updaters)
                changes += await update(dbContext);

            if (changes > 0)
                totalChanges += changes;
            else
                break;
        }
        return totalChanges;
    }

    private static IComputedExpressionAnalyzer GetComputedExpressionAnalyzer(DbContext dbContext)
    {
        if (dbContext.Model.FindRuntimeAnnotation(ComputedAnnotationNames.ExpressionAnalyzer)?.Value is not IComputedExpressionAnalyzer analyzer)
            throw new Exception($"Cannot find {ComputedAnnotationNames.ExpressionAnalyzer} for model");

        return analyzer;
    }
}

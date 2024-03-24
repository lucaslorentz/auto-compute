using System.Linq.Expressions;
using LLL.Computed.Caching;
using LLL.Computed.EFCore.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;

namespace LLL.Computed.EFCore;

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

    private static IComputedExpressionAnalyzer GetComputedExpressionAnalyzer(DbContext dbContext)
    {
        if (dbContext.Model.FindRuntimeAnnotation(ComputedAnnotationNames.ExpressionAnalyzer)?.Value is not IComputedExpressionAnalyzer analyzer)
            throw new Exception($"Cannot find {ComputedAnnotationNames.ExpressionAnalyzer} for model");

        return analyzer;
    }

    public static IAffectedEntitiesProvider GetAffectedEntitiesProvider(this DbContext dbContext, LambdaExpression computedExpression)
    {
        var analyzer = GetComputedExpressionAnalyzer(dbContext);

        var cache = dbContext.GetService<IConcurrentCreationCache>();

        var cacheKey = new ComputedExpressionAnalysisCacheKey(
            "AffectedEntities",
            new ExpressionCacheKey(computedExpression, ExpressionEqualityComparer.Instance),
            analyzer);

        return cache.GetOrCreate(
            cacheKey,
            static (k) => k.Analyzer.CreateAffectedEntitiesProvider((LambdaExpression)k.ExpressionKey.Expression));
    }

    public static Delegate GetComputedOldValueGetter(this DbContext dbContext, LambdaExpression computedExpression)
    {
        var analyzer = GetComputedExpressionAnalyzer(dbContext);

        var cache = dbContext.GetService<IConcurrentCreationCache>();

        var cacheKey = new ComputedExpressionAnalysisCacheKey(
            "OldValueFunction",
            new ExpressionCacheKey(computedExpression, ExpressionEqualityComparer.Instance),
            analyzer);

        return cache.GetOrCreate(
            cacheKey,
            static (k) => k.Analyzer.GetOldValueExpression((LambdaExpression)k.ExpressionKey.Expression).Compile());
    }

    public static Delegate GetComputedCurrentValueGetter(this DbContext dbContext, LambdaExpression computedExpression)
    {
        var analyzer = GetComputedExpressionAnalyzer(dbContext);

        var cache = dbContext.GetService<IConcurrentCreationCache>();

        var cacheKey = new ComputedExpressionAnalysisCacheKey(
            "CurrentValueFunction",
            new ExpressionCacheKey(computedExpression, ExpressionEqualityComparer.Instance),
            analyzer);

        return cache.GetOrCreate(
            cacheKey,
            static (k) => ((LambdaExpression)k.ExpressionKey.Expression).Compile());
    }

    public static async Task<IEnumerable<TEntity>> GetAffectedEntitiesAsync<TEntity, P>(
        this DbContext dbContext, Expression<Func<TEntity, P>> computedExpression)
    {
        var affectedEntitiesProvider = dbContext.GetAffectedEntitiesProvider(computedExpression);
        var affectedEntities = await affectedEntitiesProvider.GetAffectedEntitiesAsync(new EFCoreComputedInput(dbContext));
        return affectedEntities.OfType<TEntity>();
    }

    public static async Task<IEnumerable<(TEntity, P?, P?)>> GetChangesAsync<TEntity, P>(
        this DbContext dbContext, Expression<Func<TEntity, P>> computedExpression)
    {
        var oldValueGetter = dbContext.GetComputedOldValueGetter(computedExpression);
        var newValueGetter = dbContext.GetComputedCurrentValueGetter(computedExpression);

        var input = new EFCoreComputedInput(dbContext);

        var affectedEntities = await dbContext.GetAffectedEntitiesAsync(computedExpression);

        return affectedEntities.Select(e =>
        {
            var state = dbContext.Entry(e!).State;

            var oldValue = state == EntityState.Added
                ? default
                : (P?)oldValueGetter.DynamicInvoke(input, e);

            var newValue = state == EntityState.Deleted
                ? default
                : (P?)newValueGetter.DynamicInvoke(e);

            return (e, oldValue, newValue);
        }).ToArray();
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
}

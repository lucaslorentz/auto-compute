using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using LLL.ComputedExpression.Caching;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;

namespace LLL.ComputedExpression.EFCore.Internal;

public static class DbContextExtensions
{
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

    public static IAffectedEntitiesProvider<IEFCoreComputedInput, TEntity>? GetAffectedEntitiesProvider<TEntity, TValue>(this DbContext dbContext, Expression<Func<TEntity, TValue>> computedExpression)
    {
        var affectedEntitiesProvider = dbContext.GetAffectedEntitiesProvider((LambdaExpression)computedExpression)!;
        if (affectedEntitiesProvider is null)
            return null;
        return (IAffectedEntitiesProvider<IEFCoreComputedInput, TEntity>)affectedEntitiesProvider;
    }

    public static IChangesProvider? GetChangesProvider(this DbContext dbContext, LambdaExpression computedExpression)
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
            static (k) => k.Analyzer.CreateChangesProvider((LambdaExpression)k.ExpressionKey.Expression));
    }

    public static IChangesProvider<IEFCoreComputedInput, TEntity, TValue>? GetChangesProvider<TEntity, TValue>(
        this DbContext dbContext,
        Expression<Func<TEntity, TValue>> computedExpression)
        where TEntity : notnull
    {
        var changesProvider = dbContext.GetChangesProvider((LambdaExpression)computedExpression);

        if (changesProvider is null)
            return null;

        return (IChangesProvider<IEFCoreComputedInput, TEntity, TValue>)changesProvider;
    }

    private readonly static ConditionalWeakTable<DbContext, ConcurrentDictionary<IChangesProvider, IChangesProvider>> _dbContextsDeltaChangesProvider = [];

    public static IChangesProvider? GetDeltaChangesProvider(this DbContext dbContext, LambdaExpression computedExpression)
    {
        var changesProvider = dbContext.GetChangesProvider(computedExpression);
        if (changesProvider is null)
            return null;

        var deltaChangesProvider = _dbContextsDeltaChangesProvider.GetOrCreateValue(dbContext);

        return deltaChangesProvider.GetOrAdd(changesProvider, static k => k.CreateDeltaChangesProvider());
    }

    public static IChangesProvider<IEFCoreComputedInput, TEntity, TValue>? GetDeltaChangesProvider<TEntity, TValue>(
        this DbContext dbContext,
        Expression<Func<TEntity, TValue>> computedExpression)
        where TEntity : notnull
    {
        var changesProvider = dbContext.GetDeltaChangesProvider((LambdaExpression)computedExpression);

        if (changesProvider is null)
            return null;

        return (IChangesProvider<IEFCoreComputedInput, TEntity, TValue>)changesProvider;
    }

    public static IComputedExpressionAnalyzer GetComputedExpressionAnalyzer(this DbContext dbContext)
    {
        if (dbContext.Model.FindRuntimeAnnotation(ComputedAnnotationNames.ExpressionAnalyzer)?.Value is not IComputedExpressionAnalyzer analyzer)
            throw new Exception($"Cannot find {ComputedAnnotationNames.ExpressionAnalyzer} for model");

        return analyzer;
    }
}

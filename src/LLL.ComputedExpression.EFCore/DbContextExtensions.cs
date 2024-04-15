using System.Collections.Immutable;
using System.Linq.Expressions;
using LLL.ComputedExpression.Caching;
using LLL.ComputedExpression.EFCore.Internal;
using LLL.ComputedExpression.Incremental;
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

    public static async Task<IReadOnlyCollection<TEntity>> GetAffectedEntitiesAsync<TEntity, TValue>(
        this DbContext dbContext, Expression<Func<TEntity, TValue>> computedExpression)
    {
        var affectedEntitiesProvider = dbContext.GetAffectedEntitiesProvider(computedExpression);
        if (affectedEntitiesProvider is null)
            return [];

        return await affectedEntitiesProvider.GetAffectedEntitiesAsync(new EFCoreComputedInput(dbContext));
    }

    public static async Task<IReadOnlyDictionary<TEntity, IValueChange<TValue>>> GetChangesAsync<TEntity, TValue>(
        this DbContext dbContext, Expression<Func<TEntity, TValue>> computedExpression)
        where TEntity : class
    {
        var changesProvider = dbContext.GetChangesProvider(computedExpression);
        if (changesProvider is null)
            return ImmutableDictionary<TEntity, IValueChange<TValue>>.Empty;

        return await changesProvider.GetChangesAsync(new EFCoreComputedInput(dbContext));
    }

    public static async Task<IReadOnlyDictionary<TEntity, IValueChange<TValue>>> GetDeltaChangesAsync<TEntity, TValue>(
        this DbContext dbContext, Expression<Func<TEntity, TValue>> computedExpression)
        where TEntity : class
    {
        var changesProvider = dbContext.GetDeltaChangesProvider(computedExpression);
        if (changesProvider is null)
            return ImmutableDictionary<TEntity, IValueChange<TValue>>.Empty;

        return await changesProvider.GetChangesAsync(new EFCoreComputedInput(dbContext));
    }

    public static async Task<IReadOnlyDictionary<TEntity, TValue>> GetIncrementalChanges<TEntity, TValue>(
        this DbContext dbContext, IIncrementalComputed<TEntity, TValue> incrementalComputed)
        where TEntity : notnull
        where TValue : notnull
    {
        var analyzer = dbContext.GetComputedExpressionAnalyzer();

        var cache = dbContext.GetService<IConcurrentCreationCache>();

        var cacheKey = (
            "IncrementalChangesProvider",
            incrementalComputed,
            analyzer
        );

        var incrementalChangeProvider = cache.GetOrCreate(
            cacheKey,
            static (k) => (IIncrementalChangesProvider<IEFCoreComputedInput, TEntity, TValue>)k.analyzer.CreateIncrementalChangesProvider(
                k.incrementalComputed));

        var input = new EFCoreComputedInput(dbContext);

        return await incrementalChangeProvider.GetIncrementalChangesAsync(input);
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

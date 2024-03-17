using System.Linq.Expressions;
using L3.Computed.EFCore.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;

namespace L3.Computed.EFCore;

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

    public static async Task<IEnumerable<object>> GetAffectedEntitiesAsync(
        this DbContext dbContext, LambdaExpression computedExpression)
    {
        if (dbContext.Model.FindRuntimeAnnotation(ComputedAnnotationNames.ExpressionAnalyzer)?.Value is not IComputedExpressionAnalyzer analyzer)
            throw new Exception($"Cannot find {ComputedAnnotationNames.ExpressionAnalyzer} for model");

        var cache = dbContext.GetService<IAffectedEntitiesProviderCache>();
        var cacheKey = new AffectedEntitiesProviderCacheKey(computedExpression, analyzer, ExpressionEqualityComparer.Instance);
        var affectedEntitiesProvider = cache.GetOrAdd(cacheKey, () => analyzer.CreateAffectedEntitiesProvider(computedExpression));
        return await affectedEntitiesProvider.GetAffectedEntitiesAsync(new EFCoreAffectedEntitiesInput(dbContext));
    }

    public static async Task<IEnumerable<TEntity>> GetAffectedEntitiesAsync<TEntity, P>(
        this DbContext dbContext, Expression<Func<TEntity, P>> computedExpression)
    {
        var affectedEntities = await dbContext.GetAffectedEntitiesAsync((LambdaExpression)computedExpression);
        return affectedEntities.OfType<TEntity>();
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

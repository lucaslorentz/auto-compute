using System.Linq.Expressions;
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

    public static IAffectedEntitiesProvider GetAffectedEntitiesProvider(this DbContext dbContext, LambdaExpression computedExpression)
    {
        if (dbContext.Model.FindRuntimeAnnotation(ComputedAnnotationNames.ExpressionAnalyzer)?.Value is not IComputedExpressionAnalyzer analyzer)
            throw new Exception($"Cannot find {ComputedAnnotationNames.ExpressionAnalyzer} for model");

        var cache = dbContext.GetService<IAffectedEntitiesProviderCache>();
        var cacheKey = new AffectedEntitiesProviderCacheKey(computedExpression, analyzer, ExpressionEqualityComparer.Instance);
        return cache.GetOrAdd(cacheKey, () => analyzer.CreateAffectedEntitiesProvider(computedExpression));
    }

    public static async Task<IEnumerable<TEntity>> GetAffectedEntitiesAsync<TEntity, P>(
        this DbContext dbContext, Expression<Func<TEntity, P>> computedExpression)
    {
        var affectedEntitiesProvider = dbContext.GetAffectedEntitiesProvider(computedExpression);
        var affectedEntities = await affectedEntitiesProvider.GetAffectedEntitiesAsync(new EFCoreAffectedEntitiesInput(dbContext));
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

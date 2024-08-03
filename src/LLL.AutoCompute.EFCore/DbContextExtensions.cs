﻿using System.Collections.Immutable;
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
        if (dbContext.Model.FindRuntimeAnnotationValue(ComputedAnnotationNames.Updaters) is not IEnumerable<ComputedUpdater> updaters)
            throw new Exception($"Cannot find runtime annotation {ComputedAnnotationNames.Updaters} for model");

        var totalChanges = 0;
        for (var i = 0; i < 10; i++)
        {
            var autoDetectChanges = dbContext.ChangeTracker.AutoDetectChangesEnabled;
            try
            {
                dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
                dbContext.ChangeTracker.DetectChanges();

                var changes = 0;

                foreach (var update in updaters)
                    changes += await update(dbContext);

                if (changes > 0)
                    totalChanges += changes;
                else
                    break;
            }
            finally
            {
                dbContext.ChangeTracker.AutoDetectChangesEnabled = autoDetectChanges;
            }
        }
        return totalChanges;
    }

    private static readonly ConditionalWeakTable<DbContext, IEFCoreComputedInput> _inputs = [];
    internal static IEFCoreComputedInput GetComputedInput(this DbContext dbContext)
    {
        return _inputs.GetValue(dbContext, static k => new EFCoreComputedInput(k));
    }

    public static IComputedExpressionAnalyzer<IEFCoreComputedInput> GetComputedExpressionAnalyzer(this DbContext dbContext)
    {
        var annotationValue = dbContext.Model.FindRuntimeAnnotation(ComputedAnnotationNames.ExpressionAnalyzer)?.Value;

        if (annotationValue is not IComputedExpressionAnalyzer<IEFCoreComputedInput> analyzer)
            throw new Exception($"Cannot find {ComputedAnnotationNames.ExpressionAnalyzer} for model");

        return analyzer;
    }
}
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore;

public static class ComputedNavigationBuilderExtensions
{
    public static IComputedNavigationBuilder<TEntity, IEnumerable<TTarget>> ReuseItemsByKey<TEntity, TTarget>(
        this IComputedNavigationBuilder<TEntity, IEnumerable<TTarget>> builder,
        Expression<Func<TTarget, object>> keySelector)
    {
        builder.ReuseKeySelector = keySelector.Compile();
        return builder;
    }

    public static IComputedNavigationBuilder<TEntity, TTarget> ReuseItemsByKey<TEntity, TTarget>(
        this IComputedNavigationBuilder<TEntity, TTarget> builder,
        Expression<Func<TTarget, object>> keySelector)
    {
        builder.ReuseKeySelector = keySelector.Compile();
        return builder;
    }
}


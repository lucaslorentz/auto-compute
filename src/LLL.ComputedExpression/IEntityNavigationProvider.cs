using System.Linq.Expressions;

namespace LLL.Computed;

public interface IEntityNavigationProvider
{
    IExpressionMatch<IEntityNavigation>? GetEntityNavigation(Expression node);
}

public interface IEntityNavigationProvider<in TInput> : IEntityNavigationProvider
{
    new IExpressionMatch<IEntityNavigation<TInput>>? GetEntityNavigation(Expression node);

    IExpressionMatch<IEntityNavigation>? IEntityNavigationProvider.GetEntityNavigation(Expression node)
    {
        return GetEntityNavigation(node);
    }
}

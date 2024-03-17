using System.Linq.Expressions;

namespace L3.Computed;

public interface IEntityNavigationProvider
{
    IEntityNavigation? GetEntityNavigation(Expression node);
}

public interface IEntityNavigationProvider<in TInput> : IEntityNavigationProvider
{
    new IEntityNavigation<TInput>? GetEntityNavigation(Expression node);

    IEntityNavigation? IEntityNavigationProvider.GetEntityNavigation(Expression node)
    {
        return GetEntityNavigation(node);
    }
}

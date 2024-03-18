using System.Linq.Expressions;

namespace LLL.Computed;

public interface IEntityNavigation
{
    bool IsCollection { get; }
    Expression SourceExpression { get; }
    Type TargetType { get; }
    IEntityNavigationLoader GetInverseLoader();
}

public interface IEntityNavigation<in TInput> : IEntityNavigation
{
    new IEntityNavigationLoader<TInput> GetInverseLoader();

    IEntityNavigationLoader IEntityNavigation.GetInverseLoader()
    {
        return GetInverseLoader();
    }
}

using Microsoft.EntityFrameworkCore.Metadata;

namespace LLL.AutoCompute.EFCore;

public interface IComputedNavigationBuilder<TEntity, out TProperty>
{
    INavigationBase Property { get; }
    Delegate? ReuseKeySelector { get; set; }
    IList<IProperty> ReuseUpdateProperties { get; }
}


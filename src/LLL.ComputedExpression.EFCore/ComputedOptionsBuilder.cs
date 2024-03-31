using LLL.ComputedExpression.EFCore.Internal;
using Microsoft.EntityFrameworkCore;

namespace LLL.ComputedExpression.EFCore;

public class ComputedOptionsBuilder
{
    private readonly ComputedOptionsExtension _extension;

    public ComputedOptionsBuilder(DbContextOptionsBuilder optionsBuilder)
    {
        _extension = optionsBuilder.Options.FindExtension<ComputedOptionsExtension>()
            ?? new ComputedOptionsExtension();

        optionsBuilder.AddInterceptors(new ComputedInterceptor());
    }

    internal ComputedOptionsExtension Build()
    {
        return _extension;
    }
}

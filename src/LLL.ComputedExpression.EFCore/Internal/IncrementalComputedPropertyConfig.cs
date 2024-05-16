using System.Linq.Expressions;

namespace LLL.ComputedExpression.EFCore.Internal;

public class IncrementalComputedPropertyConfig
{
    public required LambdaExpression ComputedExpression { get; init; }
    public required Func<IComputedExpressionAnalyzer<IEFCoreComputedInput>, IChangeCalculation> GetChangeCalculation { get; init; }
    public required Delegate Updater { get; set; }
}
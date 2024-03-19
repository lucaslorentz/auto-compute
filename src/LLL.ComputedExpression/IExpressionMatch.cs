using System.Linq.Expressions;

namespace LLL.Computed;

public interface IExpressionMatch<out TValue>
{
    Expression FromExpression { get; }
    TValue Value { get; }
}
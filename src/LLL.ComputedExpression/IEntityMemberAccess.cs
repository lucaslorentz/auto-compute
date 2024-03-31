using System.Linq.Expressions;

namespace LLL.ComputedExpression;

public interface IEntityMemberAccess<out TMember>
{
    Expression FromExpression { get; }
    TMember Member { get; }
}
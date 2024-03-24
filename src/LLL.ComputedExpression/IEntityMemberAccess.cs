using System.Linq.Expressions;

namespace LLL.Computed;

public interface IEntityMemberAccess<out TMember>
{
    Expression FromExpression { get; }
    TMember Member { get; }
}
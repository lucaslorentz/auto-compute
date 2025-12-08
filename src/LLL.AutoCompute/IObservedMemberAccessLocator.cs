using System.Linq.Expressions;

namespace LLL.AutoCompute;

public interface IObservedMemberAccessLocator
{
    ObservedMemberAccess? GetObservedMemberAccess(Expression node);
}

using System.Linq.Expressions;

namespace LLL.AutoCompute;

public interface IObservedPropertyAccessLocator : IObservedMemberAccessLocator
{
    IObservedPropertyAccess? GetObservedPropertyAccess(Expression node);
}
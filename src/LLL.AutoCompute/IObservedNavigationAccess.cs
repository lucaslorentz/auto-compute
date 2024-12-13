namespace LLL.AutoCompute;

public interface IObservedNavigationAccess : IObservedMemberAccess
{
    IObservedNavigation Navigation { get; }
}
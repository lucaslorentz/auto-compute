namespace LLL.AutoCompute;

public interface IObservedPropertyAccess : IObservedMemberAccess
{
    IObservedProperty Property { get; }
}

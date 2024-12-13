
namespace LLL.AutoCompute;

public interface IObservedProperty : IObservedMember
{
    Type EntityType { get; }
}

public interface IObservedProperty<in TInput> : IObservedProperty, IObservedMember<TInput>
{
}
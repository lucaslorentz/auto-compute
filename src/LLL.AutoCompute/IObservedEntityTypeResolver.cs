namespace LLL.AutoCompute;

public interface IObservedEntityTypeResolver
{
    IObservedEntityType? Resolve(Type type);
}
